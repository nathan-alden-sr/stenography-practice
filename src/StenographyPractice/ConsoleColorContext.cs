using System;

namespace NathanAlden.StenographyPractice
{
    public class ConsoleColorContext : IDisposable
    {
        private readonly ConsoleColor _originalColor;

        public ConsoleColorContext(ConsoleColor color)
        {
            _originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
        }

        public void Dispose()
        {
            Console.ForegroundColor = _originalColor;
        }
    }
}