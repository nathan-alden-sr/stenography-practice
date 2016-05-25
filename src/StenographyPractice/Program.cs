using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace NathanAlden.StenographyPractice
{
    internal class Program
    {
        private static readonly string _configurationDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Stenography Practice");
        private static readonly string _dictionaryPath;
        private static DictionaryModel _model;

        static Program()
        {
            _dictionaryPath = Path.Combine(_configurationDirectory, "dictionary.json");
        }

        private static void Main()
        {
            Initialize();

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
                Console.WriteLine("5. Start flashcards");
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
                        QuizConfiguration(selectedLessons);
                        break;
                    case ConsoleKey.D5:
                    case ConsoleKey.NumPad5:
                        FlashcardsConfiguration(selectedLessons);
                        break;
                    case ConsoleKey.Escape:
                    case ConsoleKey.D6:
                    case ConsoleKey.NumPad6:
                        return;
                }
            }
        }

        private static void QuizConfiguration(IEnumerable<LessonModel> lessons)
        {
            lessons = lessons.ToArray();

            Console.WriteLine($"Lesson{(lessons.Count() != 1 ? "s" : "")}: {string.Join(", ", lessons.Select(x => x.Name))}");
            Console.WriteLine();
            Console.WriteLine("1. Display steno");
            Console.WriteLine("2. Display English");
            Console.WriteLine("3. Back");

            ConsoleKey consoleKey = Console.ReadKey(true).Key;

            switch (consoleKey)
            {
                case ConsoleKey.D1:
                case ConsoleKey.NumPad1:
                    Quiz(lessons, "English", "Steno", term => term.English, term => term.Steno, StringComparison.OrdinalIgnoreCase);
                    break;
                case ConsoleKey.D2:
                case ConsoleKey.NumPad2:
                    Quiz(lessons, "Steno", "English", term => term.Steno, term => term.English, StringComparison.Ordinal);
                    break;
                case ConsoleKey.D3:
                case ConsoleKey.NumPad3:
                case ConsoleKey.Escape:
                    Console.WriteLine();
                    return;
            }
        }

        private static void Quiz(IEnumerable<LessonModel> lessons, string questionPrompt, string answerPrompt, Func<TermModel, string> questionDelegate, Func<TermModel, string> answerDelegate, StringComparison comparisonType)
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

                string answer = Console.ReadLine();

                if (string.IsNullOrEmpty(answer))
                {
                    continue;
                }

                bool correct = string.Equals(answer, answerDelegate(term), comparisonType);

                correctAnswers += correct ? 1 : 0;
                totalAnswerAttempts++;

                Console.WriteLine();
                using (new ConsoleColorContext(correct ? ConsoleColor.Green : ConsoleColor.Red))
                {
                    Console.WriteLine(correct ? "Correct!" : "Incorrect");
                }

                if (Console.ReadKey(true).Key != ConsoleKey.Escape)
                {
                    if (correct)
                    {
                        term = terms[random.Next(0, terms.Length)];
                    }
                    continue;
                }

                Console.Clear();
                return;
            }
        }

        private static void FlashcardsConfiguration(IEnumerable<LessonModel> lessons)
        {
            lessons = lessons.ToArray();

            Console.WriteLine($"Lesson{(lessons.Count() != 1 ? "s" : "")}: {string.Join(", ", lessons.Select(x => x.Name))}");
            Console.WriteLine();
            Console.Write("Interval in milliseconds: ");

            string line = Console.ReadLine();
            int intervalInMilliseconds;

            if (!int.TryParse(line, out intervalInMilliseconds))
            {
                return;
            }

            TimeSpan interval = TimeSpan.FromMilliseconds(intervalInMilliseconds);

            Console.WriteLine();
            Console.WriteLine("1. Display steno first");
            Console.WriteLine("2. Display English first");
            Console.WriteLine("3. Back");

            ConsoleKey consoleKey = Console.ReadKey(true).Key;

            switch (consoleKey)
            {
                case ConsoleKey.D1:
                case ConsoleKey.NumPad1:
                    Flashcards(lessons, interval, "  Steno", "English", term => term.Steno, term => term.English);
                    break;
                case ConsoleKey.D2:
                case ConsoleKey.NumPad2:
                    Flashcards(lessons, interval, "English", "  Steno", term => term.English, term => term.Steno);
                    break;
                case ConsoleKey.D3:
                case ConsoleKey.NumPad3:
                case ConsoleKey.Escape:
                    Console.WriteLine();
                    return;
            }
        }

        private static void Flashcards(IEnumerable<LessonModel> lessons, TimeSpan interval, string frontDefinition, string backDefinition, Func<TermModel, string> frontDelegate, Func<TermModel, string> backDelegate)
        {
            lessons = lessons.ToArray();

            TermModel[] terms = lessons.SelectMany(x => x.Terms).ToArray();
            var random = new Random();
            string flashcardLogPath = Path.Combine(_configurationDirectory, "flashcards.csv");

            using (var streamWriter = new StreamWriter(flashcardLogPath, false, Encoding.ASCII))
            {
                do
                {
                    TermModel term = terms[random.Next(0, terms.Length)];
                    string frontText = frontDelegate(term);
                    string backText = backDelegate(term);

                    streamWriter.WriteLine($"{frontText}\t{backText}");

                    Console.Clear();

                    Console.WriteLine($"Lesson{(lessons.Count() != 1 ? "s" : "")}: {string.Join(", ", lessons.Select(x => x.Name))}");
                    Console.WriteLine();
                    Console.WriteLine($"{frontDefinition}: {frontText}");

                    Thread.Sleep(interval);

                    if (Console.KeyAvailable)
                    {
                        break;
                    }

                    Console.WriteLine($"{backDefinition}: {backText}");

                    Thread.Sleep(interval);
                } while (!Console.KeyAvailable);
            }

            DrainKeys();

            Console.Clear();
        }

        private static void Initialize()
        {
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
                                                           new TermModel { English = "English", Steno = "Steno" },
                                                           new TermModel { English = "English", Steno = "Steno" }
                                                       }
                                           },
                                           new LessonModel
                                           {
                                               Name = "Lesson 2",
                                               Terms = new[]
                                                       {
                                                           new TermModel { English = "English", Steno = "Steno" },
                                                           new TermModel { English = "English", Steno = "Steno" }
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

            Console.Title = "Stenography Practice";
        }

        private static void DrainKeys()
        {
            while (Console.KeyAvailable)
            {
                Console.ReadKey(true);
            }
        }
    }
}