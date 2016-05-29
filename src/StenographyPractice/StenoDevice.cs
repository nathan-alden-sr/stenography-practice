using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace NathanAlden.StenographyPractice
{
    public class StenoDevice
    {
        private const int VendorId = 0x112b;
        private const int ProductId = 0x000d;
        private const string Name = "Steno Machine";
        private const int UsbPacketDataSize = 0x10000;
        private const ReadEndpointID ReadEndpointId = ReadEndpointID.Ep01;
        private const WriteEndpointID WriteEndpointId = WriteEndpointID.Ep02;
        private static readonly TimeSpan _waitTimeout = TimeSpan.FromMilliseconds(100);
        private static readonly int _transferTimeoutInMilliseconds = 2000;
        private static readonly int _usbPacketSize = Marshal.SizeOf<UsbPacket>();
        private static readonly int _usbPacketSizeWithoutData = Marshal.SizeOf<UsbPacket>() - UsbPacketDataSize;
        private static readonly TimeSpan _pollDelay = TimeSpan.FromMilliseconds(100);
        private readonly Subject<ErrorCode> _error = new Subject<ErrorCode>();
        private readonly Subject<int> _incorrectNumberOfBytesReceived = new Subject<int>();
        private readonly Subject<IncorrectSequenceNumberReceivedArgs> _incorrectSequenceNumberReceived = new Subject<IncorrectSequenceNumberReceivedArgs>();
        private readonly Subject<Unit> _noData = new Subject<Unit>();
        private readonly Subject<Stroke> _strokeReceived = new Subject<Stroke>();
        private CancellationTokenSource _cancellationTokenSource;
        private uint _fileOffset;
        private uint _sequenceNumber = 1;
        private UsbDevice _stenoDevice;

        private StenoDevice(UsbDevice stenoDevice)
        {
            _stenoDevice = stenoDevice;
        }

        public IObservable<ErrorCode> Error => _error.AsObservable();
        public IObservable<int> IncorrectNumberOfBytesReceived => _incorrectNumberOfBytesReceived.AsObservable();
        public IObservable<IncorrectSequenceNumberReceivedArgs> IncorrectSequenceNumberReceived => _incorrectSequenceNumberReceived.AsObservable();
        public IObservable<Unit> NoData => _noData.AsObservable();
        public IObservable<Stroke> StrokeReceived => _strokeReceived.AsObservable();

        public static StenoDevice Open()
        {
            UsbRegDeviceList devices = UsbDevice.AllDevices;
            UsbRegistry usbRegistry = devices.Find(x => x.Vid == VendorId && x.Pid == ProductId && x.Name == Name);

            if (usbRegistry == null)
            {
                return null;
            }

            UsbDevice stenoDevice;

            if (!usbRegistry.Open(out stenoDevice))
            {
                return null;
            }

            var wholeUsbDevice = stenoDevice as IUsbDevice;

            if (wholeUsbDevice != null)
            {
                wholeUsbDevice.SetConfiguration(1);
                wholeUsbDevice.ClaimInterface(0);
            }

            return new StenoDevice(stenoDevice);
        }

        public async Task FlushAsync()
        {
            StartReadingStrokes();

            var cancellationTokenSource = new CancellationTokenSource();

            IDisposable error = _error.Subscribe(errorCode => cancellationTokenSource.Cancel());
            IDisposable noData = _noData.Subscribe(errorCode => cancellationTokenSource.Cancel());

            try
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(1000), cancellationTokenSource.Token);
                    }
                    catch (TaskCanceledException)
                    {
                    }
                }
            }
            finally
            {
                error.Dispose();
                noData.Dispose();

                StopReadingStrokes();
            }
        }

        public void StartReadingStrokes()
        {
            ValidateState();

            if (_cancellationTokenSource != null)
            {
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();

            Task.Run(
                async () =>
                      {
                          using (UsbEndpointReader endpointReader = _stenoDevice.OpenEndpointReader(ReadEndpointId))
                          using (UsbEndpointWriter endpointWriter = _stenoDevice.OpenEndpointWriter(WriteEndpointId))
                          {
                              while (!_cancellationTokenSource.IsCancellationRequested)
                              {
                                  ErrorCode errorCode = WriteReadBytesCommand(endpointWriter);

                                  if (errorCode == ErrorCode.None)
                                  {
                                      ReadUsbPacketResult result = ReadUsbPacket(endpointReader);

                                      if (result.ErrorCode == ErrorCode.None)
                                      {
                                          foreach (Stroke stroke in result.Strokes)
                                          {
                                              _strokeReceived.OnNext(stroke);
                                          }
                                      }
                                      else
                                      {
                                          _error.OnNext(result.ErrorCode);
                                      }
                                  }
                                  else
                                  {
                                      _error.OnNext(errorCode);
                                  }

                                  await Task.Delay(_pollDelay, _cancellationTokenSource.Token);
                              }
                          }
                      },
                _cancellationTokenSource.Token);
        }

        public void StopReadingStrokes()
        {
            ValidateState();

            if (_cancellationTokenSource == null)
            {
                return;
            }

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = null;
        }

        public void Close()
        {
            if (_stenoDevice == null)
            {
                return;
            }

            StopReadingStrokes();

            if (_stenoDevice.IsOpen)
            {
                var wholeUsbDevice = _stenoDevice as IUsbDevice;

                wholeUsbDevice?.ReleaseInterface(0);

                _stenoDevice.Close();
                _stenoDevice = null;
            }

            UsbDevice.Exit();
        }

        private void ValidateState()
        {
            if (_stenoDevice == null)
            {
                throw new InvalidOperationException("Steno device is closed.");
            }
        }

        private ErrorCode WriteReadBytesCommand(UsbEndpointBase endpoint)
        {
            UsbPacket usbPacket = CreateReadBytesCommandPacket();
            UsbTransfer transfer = null;

            try
            {
                ErrorCode errorCode = endpoint.SubmitAsyncTransfer(usbPacket, 0, (int)(_usbPacketSizeWithoutData + usbPacket.uiDataLen), _transferTimeoutInMilliseconds, out transfer);

                if (errorCode != ErrorCode.None)
                {
                    return errorCode;
                }

                WaitHandle.WaitAll(new[] { transfer.AsyncWaitHandle }, _waitTimeout, false);

                if (!transfer.IsCompleted)
                {
                    transfer.Cancel();
                }

                int bytesTransferred;

                return transfer.Wait(out bytesTransferred);
            }
            finally
            {
                transfer?.Dispose();
            }
        }

        private unsafe ReadUsbPacketResult ReadUsbPacket(UsbEndpointBase endpoint)
        {
            UsbTransfer transfer = null;

            try
            {
                var usbPacketBuffer = new byte[_usbPacketSize];
                ErrorCode errorCode = endpoint.SubmitAsyncTransfer(usbPacketBuffer, 0, usbPacketBuffer.Length, _transferTimeoutInMilliseconds, out transfer);

                if (errorCode != ErrorCode.None)
                {
                    _error.OnNext(errorCode);

                    return new ReadUsbPacketResult(errorCode);
                }

                WaitHandle.WaitAll(new[] { transfer.AsyncWaitHandle }, _waitTimeout, false);

                if (!transfer.IsCompleted)
                {
                    transfer.Cancel();
                }

                int bytesTransferred;

                errorCode = transfer.Wait(out bytesTransferred);

                if (bytesTransferred == 0)
                {
                    return new ReadUsbPacketResult(errorCode);
                }

                GCHandle gcHandle = GCHandle.Alloc(usbPacketBuffer, GCHandleType.Pinned);
                var usbPacket = Marshal.PtrToStructure<UsbPacket>(gcHandle.AddrOfPinnedObject());

                if (usbPacket.uiSeqNum != _sequenceNumber)
                {
                    _incorrectSequenceNumberReceived.OnNext(new IncorrectSequenceNumberReceivedArgs(_sequenceNumber, usbPacket.uiSeqNum));

                    return new ReadUsbPacketResult(errorCode);
                }

                _sequenceNumber++;

                if (bytesTransferred < _usbPacketSizeWithoutData)
                {
                    _incorrectNumberOfBytesReceived.OnNext(bytesTransferred);

                    return new ReadUsbPacketResult(errorCode);
                }
                if (errorCode != ErrorCode.None)
                {
                    return new ReadUsbPacketResult(errorCode);
                }
                if (usbPacket.uiDataLen == 0)
                {
                    _noData.OnNext(Unit.Default);

                    return new ReadUsbPacketResult(errorCode);
                }

                _fileOffset += usbPacket.uiDataLen;

                var strokes = new List<Stroke>();
                var dataBuffer = new byte[usbPacket.uiDataLen];
                Marshal.Copy(new IntPtr(usbPacket.pData), dataBuffer, 0, dataBuffer.Length);

                for (var i = 0; i < dataBuffer.Length; i += 8)
                {
                    strokes.Add(new Stroke(dataBuffer[i], dataBuffer[i + 1], dataBuffer[i + 2], dataBuffer[i + 3]));
                }

                return new ReadUsbPacketResult(errorCode, strokes);
            }
            finally
            {
                transfer?.Dispose();
            }
        }

        private unsafe UsbPacket CreateReadBytesCommandPacket()
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            var usbPacket = new UsbPacket
                            {
                                uiSeqNum = _sequenceNumber,
                                usPacketType = 0x0013,
                                uiDataLen = 0,
                                ReadBytes = new ReadBytesParams
                                            {
                                                uiFileOffset = _fileOffset,
                                                uiByteCount = 512
                                            }
                            };

            usbPacket.ucSync[0] = (byte)'S';
            usbPacket.ucSync[1] = (byte)'G';
            usbPacket.ReadBytes.uiUnused[0] = 0;
            usbPacket.ReadBytes.uiUnused[1] = 0;
            usbPacket.ReadBytes.uiUnused[2] = 0;

            return usbPacket;
        }

        public class IncorrectSequenceNumberReceivedArgs
        {
            public IncorrectSequenceNumberReceivedArgs(uint requestedSequenceNumber, uint receivedSequenceNumber)
            {
                RequestedSequenceNumber = requestedSequenceNumber;
                ReceivedSequenceNumber = receivedSequenceNumber;
            }

            public uint RequestedSequenceNumber { get; }
            public uint ReceivedSequenceNumber { get; }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
        private unsafe struct UsbPacket
        {
            public fixed byte ucSync [2];
            public uint uiSeqNum;
            public ushort usPacketType;
            public uint uiDataLen;
            public ReadBytesParams ReadBytes;
            public fixed byte pData [UsbPacketDataSize];
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
        private unsafe struct ReadBytesParams
        {
            public uint uiFileOffset;
            public uint uiByteCount;
            public fixed uint uiUnused [3];
        }

        private class ReadUsbPacketResult
        {
            public ReadUsbPacketResult(ErrorCode errorCode, IEnumerable<Stroke> strokes = null)
            {
                ErrorCode = errorCode;
                Strokes = strokes ?? Enumerable.Empty<Stroke>();
            }

            public ErrorCode ErrorCode { get; }
            public IEnumerable<Stroke> Strokes { get; }
        }
    }
}