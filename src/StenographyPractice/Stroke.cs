using System.Text;

namespace NathanAlden.StenographyPractice
{
    public class Stroke
    {
        public Stroke(byte byte0, byte byte1, byte byte2, byte byte3)
        {
            StenomarkMarker = GetBit(byte0, 2);
            NumeralBar = GetBit(byte0, 3);
            LeftS = GetBit(byte0, 4);
            LeftT = GetBit(byte0, 5);
            K = GetBit(byte0, 6);
            LeftP = GetBit(byte0, 7);

            W = GetBit(byte1, 2);
            H = GetBit(byte1, 3);
            LeftR = GetBit(byte1, 4);
            A = GetBit(byte1, 5);
            O = GetBit(byte1, 6);
            Asterisk = GetBit(byte1, 7);

            E = GetBit(byte2, 2);
            U = GetBit(byte2, 3);
            F = GetBit(byte2, 4);
            RightR = GetBit(byte2, 5);
            RightP = GetBit(byte2, 6);
            B = GetBit(byte2, 7);

            L = GetBit(byte3, 2);
            G = GetBit(byte3, 3);
            RightT = GetBit(byte3, 4);
            RightS = GetBit(byte3, 5);
            D = GetBit(byte3, 6);
            Z = GetBit(byte3, 7);
        }

        public bool StenomarkMarker { get; }
        public bool NumeralBar { get; }
        public bool LeftS { get; }
        public bool LeftT { get; }
        public bool K { get; }
        public bool LeftP { get; }
        public bool W { get; }
        public bool H { get; }
        public bool LeftR { get; }
        public bool A { get; }
        public bool O { get; }
        public bool Asterisk { get; }
        public bool E { get; }
        public bool U { get; }
        public bool F { get; }
        public bool RightR { get; }
        public bool RightP { get; }
        public bool B { get; }
        public bool L { get; }
        public bool G { get; }
        public bool RightT { get; }
        public bool RightS { get; }
        public bool D { get; }
        public bool Z { get; }

        public string Steno
        {
            get
            {
                var stringBuilder = new StringBuilder();
                bool rightHand = E || U || F || RightR || RightP || B || L || G || RightT || RightS || D || Z;

                if (LeftS)
                {
                    stringBuilder.Append(NumeralBar ? '1' : 'S');
                }
                if (LeftT)
                {
                    stringBuilder.Append(NumeralBar ? '2' : 'T');
                }
                if (K)
                {
                    stringBuilder.Append('K');
                }
                if (LeftP)
                {
                    stringBuilder.Append(NumeralBar ? '3' : 'P');
                }
                if (W)
                {
                    stringBuilder.Append('W');
                }
                if (H)
                {
                    stringBuilder.Append(NumeralBar ? '4' : 'H');
                }
                if (LeftR)
                {
                    stringBuilder.Append('R');
                }
                if (A)
                {
                    stringBuilder.Append(NumeralBar ? '5' : 'A');
                }
                if (O)
                {
                    stringBuilder.Append(NumeralBar ? '0' : 'O');
                }
                if (Asterisk)
                {
                    stringBuilder.Append('*');
                }
                else if (rightHand && !A && !O && !E && !U)
                {
                    stringBuilder.Append('-');
                }
                if (E)
                {
                    stringBuilder.Append('E');
                }
                if (U)
                {
                    stringBuilder.Append('U');
                }
                if (F)
                {
                    stringBuilder.Append(NumeralBar ? '6' : 'F');
                }
                if (RightR)
                {
                    stringBuilder.Append('R');
                }
                if (RightP)
                {
                    stringBuilder.Append(NumeralBar ? '7' : 'P');
                }
                if (B)
                {
                    stringBuilder.Append('B');
                }
                if (L)
                {
                    stringBuilder.Append(NumeralBar ? '8' : 'L');
                }
                if (G)
                {
                    stringBuilder.Append('G');
                }
                if (RightT)
                {
                    stringBuilder.Append(NumeralBar ? '9' : 'T');
                }
                if (RightS)
                {
                    stringBuilder.Append('S');
                }
                if (D)
                {
                    stringBuilder.Append('D');
                }
                if (Z)
                {
                    stringBuilder.Append('Z');
                }

                return stringBuilder.ToString();
            }
        }

        private static bool GetBit(byte @byte, int index)
        {
            return (@byte & (0x1 << (7 - index))) != 0;
        }
    }
}