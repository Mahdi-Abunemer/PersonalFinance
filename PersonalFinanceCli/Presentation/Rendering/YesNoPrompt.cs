using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonalFinanceCli.Presentation.Rendering
{
    public sealed class YesNoPrompt
    {
        private readonly IConsole _console;

        public YesNoPrompt(IConsole console)
        {
            _console = console;
        }

        public bool AskYesNo(string prompt)
        {
            while (true)
            {
                string? rawAnswer = ReadAnswer(prompt);
                if (rawAnswer == null)
                {
                    return false;
                }

                var normalizedAnswer = rawAnswer.Trim();
                if (IsYes(normalizedAnswer))
                    return true;

                if (IsNo(normalizedAnswer))
                    return false;

                _console.WriteLine("Error: Please answer y/n.");
            }
        }

        public bool AskYesNoDefaultYes(string prompt)
        {
            while (true)
            {
                string? rawAnswer = ReadAnswer(prompt);
                if (rawAnswer == null)
                {
                    return false;
                }

                var normalizedAnswer = rawAnswer.Trim();
                if (normalizedAnswer.Length == 0)
                {
                    return true;
                }

                if (IsYes(normalizedAnswer))
                    return true;

                if (IsNo(normalizedAnswer))
                    return false;

                _console.WriteLine("Error: Please answer y/n.");
            }
        }

        public bool AskYesNoDefaultNo(string prompt)
        {
            while (true)
            {
                string? rawAnswer = ReadAnswer(prompt);
                if (rawAnswer == null)
                {
                    return false;
                }

                var normalizedAnswer = rawAnswer.Trim();
                if (normalizedAnswer.Length == 0)
                {
                    return false;
                }

                if (IsYes(normalizedAnswer))
                    return true;

                if (IsNo(normalizedAnswer))
                    return false;

                _console.WriteLine("Error: Please answer y/n.");
            }
        }

        public bool AskYesNoWithCancel(string prompt, out bool isCanceled)
        {
            isCanceled = false;

            while (true)
            {
                string? rawAnswer = ReadAnswer(prompt);
                if (rawAnswer == null)
                {
                    return false;
                }

                var normalizedAnswer = rawAnswer.Trim();
                if (normalizedAnswer.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                {
                    isCanceled = true;
                    return false;
                }

                if (normalizedAnswer.Length == 0)
                {
                    return false;
                }

                if (IsYes(normalizedAnswer))
                    return true;

                if (IsNo(normalizedAnswer))
                    return false;

                _console.WriteLine("Error: Please answer y/n.");
            }
        }

        private static bool IsYes(string answer)
        {
            return answer.Equals("y", StringComparison.OrdinalIgnoreCase)
                || answer.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNo(string answer)
        {
            return answer.Equals("n", StringComparison.OrdinalIgnoreCase)
                || answer.Equals("no", StringComparison.OrdinalIgnoreCase);
        }

        private string? ReadAnswer(string prompt)
        {
            _console.Write($"{prompt} ");

            var rawAnswer = _console.ReadLine();
            return rawAnswer;
        }
    }
}
