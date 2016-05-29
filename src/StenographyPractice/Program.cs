using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NathanAlden.StenographyPractice
{
    internal class Program
    {
        private static readonly string _configurationDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Stenography Practice");
        private static readonly string _dictionaryPath;
        private static DictionaryModel _model;
        private static StenoDevice _stenoDevice;

        static Program()
        {
            _dictionaryPath = Path.Combine(_configurationDirectory, "dictionary.json");
        }

        private static void Main()
        {
            if (!InitializeAsync().GetAwaiter().GetResult())
            {
                return;
            }

            try
            {
                Console.WriteLine("Stenography Practice");
                Console.WriteLine();
                Console.WriteLine($"Dictionary path: {_dictionaryPath}");
                Console.WriteLine();

                List<LessonModel> selectedLessons = _model.Lessons.ToList();

                while (true)
                {
                    Console.WriteLine($"Selected lesson{(selectedLessons.Count != 1 ? "s" : "")}: {(selectedLessons.Count == _model.Lessons.Count() ? "All" : string.Join(", ", selectedLessons.Select(x => x.Name)))}");
                    Console.WriteLine();
                    Console.WriteLine("1. Show lessons");
                    Console.WriteLine("2. Select specific lessons");
                    Console.WriteLine("3. Select all lessons");
                    Console.WriteLine("4. Start quiz");
                    Console.WriteLine("5. Test steno machine");
                    Console.WriteLine("6. Exit");
                    ConsoleKey consoleKey = Console.ReadKey(true).Key;

                    Console.WriteLine();

                    switch (consoleKey)
                    {
                        case ConsoleKey.D1:
                        case ConsoleKey.NumPad1:
                            foreach (string name in _model.Lessons.Select(x => x.Name))
                            {
                                Console.WriteLine(name);
                            }
                            Console.WriteLine();
                            break;
                        case ConsoleKey.D2:
                        case ConsoleKey.NumPad2:
                            Console.Write("Enter the lesson names (comma-delimited): ");

                            string line = Console.ReadLine();
                            string[] parsedLine = line?.Split(',');

                            if (parsedLine != null && parsedLine.All(x => _model.Lessons.Any(y => y.Name.Equals(x, StringComparison.OrdinalIgnoreCase))))
                            {
                                selectedLessons = _model.Lessons.Where(x => parsedLine.Contains(x.Name, StringComparer.OrdinalIgnoreCase)).ToList();
                            }
                            else
                            {
                                Console.WriteLine();
                                Console.WriteLine("One or more lessons are invalid");
                            }

                            Console.WriteLine();
                            break;
                        case ConsoleKey.D3:
                        case ConsoleKey.NumPad3:
                            selectedLessons = _model.Lessons.ToList();
                            break;
                        case ConsoleKey.D4:
                        case ConsoleKey.NumPad4:
                            QuizConfigurationAsync(selectedLessons).GetAwaiter().GetResult();
                            break;
                        case ConsoleKey.D5:
                        case ConsoleKey.NumPad5:
                            TestStenoMachineAsync().GetAwaiter().GetResult();
                            break;
                        case ConsoleKey.Escape:
                        case ConsoleKey.D6:
                        case ConsoleKey.NumPad6:
                            return;
                    }
                }
            }
            finally
            {
                _stenoDevice.Close();
            }
        }

        private static async Task<bool> InitializeAsync()
        {
            Console.Title = "Stenography Practice";

            if (!Directory.Exists(_configurationDirectory))
            {
                Directory.CreateDirectory(_configurationDirectory);
            }
            if (!File.Exists(_dictionaryPath))
            {
                _model = new DictionaryModel
                         {
                             Lessons = new[]
                                       {
                                           new LessonModel
                                           {
                                               Name = "Lesson 1",
                                               Terms = new[]
                                                       {
                                                           new TermModel { English = "require", Steno = "/RAOEUR" },
                                                           new TermModel { English = "rather", Steno = "/RER" },
                                                           new TermModel { English = "within", Steno = "/W-PB" },
                                                           new TermModel { English = "fifth", Steno = "TPEUF/-GT" }
                                                       }
                                           },
                                           new LessonModel
                                           {
                                               Name = "Lesson 2",
                                               Terms = new[]
                                                       {
                                                           new TermModel { English = "you", Steno = "/U" },
                                                           new TermModel { English = "I", Steno = "/EU" },
                                                           new TermModel { English = "is the", Steno = "/S-T" },
                                                           new TermModel { English = "month", Steno = "PHOPB/-GT" }
                                                       }
                                           }
                                       }
                         };

                File.WriteAllText(_dictionaryPath, JsonConvert.SerializeObject(_model, Formatting.Indented), Encoding.UTF8);
            }
            else
            {
                _model = JsonConvert.DeserializeObject<DictionaryModel>(File.ReadAllText(_dictionaryPath, Encoding.UTF8));
            }

            Console.Clear();

            Console.WriteLine("Connecting to steno machine");

            _stenoDevice = StenoDevice.Open();

            if (_stenoDevice != null)
            {
                Console.WriteLine("Flushing steno machine");

                await _stenoDevice.FlushAsync();

                Console.Clear();

                return true;
            }

            Console.WriteLine("Steno machine not found");

            return false;
        }

        private static async Task QuizConfigurationAsync(IEnumerable<LessonModel> lessons)
        {
            lessons = lessons.ToArray();

            Console.WriteLine($"Lesson{(lessons.Count() != 1 ? "s" : "")}: {string.Join(", ", lessons.Select(x => x.Name))}");
            Console.WriteLine();
            Console.WriteLine("1. Display English, prompt for steno");
            Console.WriteLine("2. Display steno, prompt for English");
            Console.WriteLine("3. Back");

            ConsoleKey consoleKey = Console.ReadKey(true).Key;

            switch (consoleKey)
            {
                case ConsoleKey.D1:
                case ConsoleKey.NumPad1:
                    await QuizAsync(lessons, "English", "  Steno", term => term.English, term => term.Steno, ReadAnswerFromStenoDeviceAsync, StringComparison.OrdinalIgnoreCase);
                    break;
                case ConsoleKey.D2:
                case ConsoleKey.NumPad2:
                    await QuizAsync(lessons, "  Steno", "English", term => term.Steno, term => term.English, ReadAnswerFromConsoleAsync, StringComparison.Ordinal);
                    break;
                case ConsoleKey.D3:
                case ConsoleKey.NumPad3:
                case ConsoleKey.Escape:
                    Console.WriteLine();
                    return;
            }
        }

        private static async Task QuizAsync(
            IEnumerable<LessonModel> lessons,
            string questionPrompt,
            string answerPrompt,
            Func<TermModel, string> questionDelegate,
            Func<TermModel, string> answerDelegate,
            Func<TermModel, Task<PromptResult>> promptDelegate,
            StringComparison comparisonType)
        {
            lessons = lessons.ToArray();

            TermModel[] terms = lessons.SelectMany(x => x.Terms).ToArray();
            var random = new Random();
            var correctAnswers = 0;
            var totalAnswerAttempts = 0;
            TermModel term = terms[random.Next(0, terms.Length)];

            while (true)
            {
                Console.Clear();

                Console.WriteLine($"Lesson{(lessons.Count() != 1 ? "s" : "")}: {string.Join(", ", lessons.Select(x => x.Name))}");
                Console.WriteLine();
                if (totalAnswerAttempts > 0)
                {
                    Console.WriteLine($"{correctAnswers} / {totalAnswerAttempts} = {(correctAnswers / (decimal)totalAnswerAttempts * 100).ToString("#0.0")}%");
                    Console.WriteLine();
                }

                Console.WriteLine($"{questionPrompt}: {questionDelegate(term)}");
                Console.Write($"{answerPrompt}: ");

                PromptResult result = await promptDelegate(term);

                if (result.Type == PromptResultType.Canceled)
                {
                    Console.Clear();
                    return;
                }
                if (string.IsNullOrEmpty(result.Answer))
                {
                    continue;
                }

                bool correct = string.Equals(result.Answer, answerDelegate(term), comparisonType);

                correctAnswers += correct ? 1 : 0;
                totalAnswerAttempts++;

                Console.WriteLine();
                using (new ConsoleColorContext(correct ? ConsoleColor.Green : ConsoleColor.Red))
                {
                    Console.WriteLine(correct ? "Correct!" : "Incorrect");
                }

                Thread.Sleep(TimeSpan.FromSeconds(2));

                if (correct)
                {
                    term = terms[random.Next(0, terms.Length)];
                }
            }
        }

        private static async Task TestStenoMachineAsync()
        {
            await _stenoDevice.FlushAsync();

            Console.Clear();
            Console.WriteLine("Steno machine test");
            Console.WriteLine();

            IDisposable strokeReceived = _stenoDevice.StrokeReceived.Subscribe(stroke => { Console.WriteLine($"/{stroke.Steno}"); });
            IDisposable error = _stenoDevice.Error.Subscribe(errorCode => { Console.WriteLine($"Error: {errorCode}"); });

            try
            {
                _stenoDevice.StartReadingStrokes();

                while (!Console.KeyAvailable || Console.ReadKey(true).Key != ConsoleKey.Escape)
                {
                }
            }
            finally
            {
                strokeReceived.Dispose();
                error.Dispose();

                _stenoDevice.StopReadingStrokes();
            }

            Console.Clear();
        }

        private static Task<PromptResult> ReadAnswerFromConsoleAsync(TermModel term)
        {
            string line = Console.ReadLine();

            return Task.FromResult(string.IsNullOrEmpty(line) ? PromptResult.Canceled() : PromptResult.Success(line));
        }

        private static async Task<PromptResult> ReadAnswerFromStenoDeviceAsync(TermModel term)
        {
            string[] strokesAsSteno = term.Steno.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var strokes = new List<Stroke>();
            var cancellationTokenSource = new CancellationTokenSource();
            IDisposable strokesReceived = _stenoDevice.StrokeReceived.Subscribe(
                stroke =>
                {
                    strokes.Add(stroke);
                    if (strokes.Count == strokesAsSteno.Length)
                    {
                        cancellationTokenSource.Cancel();
                    }
                });

            _stenoDevice.StartReadingStrokes();

            try
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), cancellationTokenSource.Token);
                    }
                    catch (TaskCanceledException)
                    {
                    }
                }
            }
            finally
            {
                strokesReceived.Dispose();

                _stenoDevice.StopReadingStrokes();
            }

            string steno = strokes.Count == 1 ? $"/{strokes[0].Steno}" : string.Join("/", strokes.Select(x => x.Steno));

            Console.WriteLine(steno);

            return PromptResult.Success(steno);
        }

        private class PromptResult
        {
            private PromptResult(PromptResultType type, string answer = null)
            {
                Type = type;
                Answer = answer;
            }

            public PromptResultType Type { get; }
            public string Answer { get; }

            public static PromptResult Success(string answer)
            {
                return new PromptResult(PromptResultType.Success, answer);
            }

            public static PromptResult Canceled()
            {
                return new PromptResult(PromptResultType.Canceled);
            }
        }

        private enum PromptResultType
        {
            Success,
            Canceled
        }
    }
}