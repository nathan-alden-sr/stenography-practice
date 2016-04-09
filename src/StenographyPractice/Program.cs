using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace NathanAlden.StenographyPractice
{
    internal class Program
    {
        private static readonly string _dictionaryDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Stenography Practice");
        private static readonly string _dictionaryPath;
        private static DictionaryModel _model;

        static Program()
        {
            _dictionaryPath = Path.Combine(_dictionaryDirectory, "dictionary.json");
        }

        private static void Main()
        {
            Initialize();

            Console.WriteLine("Stenography Practice");
            Console.WriteLine();
            Console.WriteLine($"Dictionary path: {_dictionaryPath}");
            Console.WriteLine();

            string lesson = null;

            while (true)
            {
                Console.WriteLine($"Current lesson: {lesson ?? "All"}");
                Console.WriteLine();
                Console.WriteLine("1. Show lessons");
                Console.WriteLine("2. Select a specific lesson");
                Console.WriteLine("3. Select all lessons");
                Console.WriteLine("4. Start practice");
                Console.WriteLine("5. Exit");
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
                        Console.Write("Enter the lesson name: ");

                        string newLesson = Console.ReadLine();

                        if (!_model.Lessons.Any(x => string.Equals(x.Name, newLesson, StringComparison.OrdinalIgnoreCase)))
                        {
                            Console.WriteLine();
                            Console.WriteLine("Invalid lesson");
                        }
                        else
                        {
                            lesson = newLesson;
                        }

                        Console.WriteLine();
                        break;
                    case ConsoleKey.D3:
                    case ConsoleKey.NumPad3:
                        lesson = null;
                        break;
                    case ConsoleKey.D4:
                    case ConsoleKey.NumPad4:
                        QuizType(lesson != null ? new[] { _model.Lessons.Single(x => string.Equals(x.Name, lesson, StringComparison.OrdinalIgnoreCase)) } : _model.Lessons);
                        break;
                    case ConsoleKey.Escape:
                    case ConsoleKey.D5:
                    case ConsoleKey.NumPad5:
                        return;
                }
            }
        }

        private static void QuizType(IEnumerable<LessonModel> models)
        {
            models = models.ToArray();

            Console.WriteLine($"Lesson(s): {string.Join(", ", models.Select(x => x.Name))}");
            Console.WriteLine();
            Console.WriteLine("1. Display steno");
            Console.WriteLine("2. Display English");
            Console.WriteLine("3. Back");

            ConsoleKey consoleKey = Console.ReadKey(true).Key;

            switch (consoleKey)
            {
                case ConsoleKey.D1:
                case ConsoleKey.NumPad1:
                    Quiz(models, "English", "Steno", term => term.English, term => term.Steno, StringComparison.OrdinalIgnoreCase);
                    break;
                case ConsoleKey.D2:
                case ConsoleKey.NumPad2:
                    Quiz(models, "Steno", "English", term => term.Steno, term => term.English, StringComparison.Ordinal);
                    break;
                case ConsoleKey.D3:
                case ConsoleKey.NumPad3:
                case ConsoleKey.Escape:
                    Console.WriteLine();
                    return;
            }
        }

        private static void Quiz(IEnumerable<LessonModel> models, string questionPrompt, string answerPrompt, Func<TermModel, string> questionDelegate, Func<TermModel, string> answerDelegate, StringComparison comparisonType)
        {
            models = models.ToArray();

            TermModel[] terms = models.SelectMany(x => x.Terms).ToArray();
            var random = new Random();

            while (true)
            {
                Console.Clear();

                Console.WriteLine($"Lesson(s): {string.Join(", ", models.Select(x => x.Name))}");
                Console.WriteLine();

                TermModel term = terms[random.Next(0, terms.Length)];

                Console.WriteLine($"{questionPrompt}: {questionDelegate(term)}");
                Console.Write($"{answerPrompt}: ");

                string answer = Console.ReadLine();

                if (string.IsNullOrEmpty(answer))
                {
                    Console.Clear();
                    return;
                }

                Console.WriteLine();
                Console.WriteLine(string.Equals(answer, answerDelegate(term), comparisonType) ? "Correct!" : "Incorrect");

                if (Console.ReadKey(true).Key != ConsoleKey.Escape)
                {
                    continue;
                }

                Console.Clear();
                return;
            }
        }

        private static void Initialize()
        {
            if (!Directory.Exists(_dictionaryDirectory))
            {
                Directory.CreateDirectory(_dictionaryDirectory);
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
    }
}