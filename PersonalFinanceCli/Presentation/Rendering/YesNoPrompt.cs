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
                _console.Write($"{prompt} ");

                var rawAnswer = _console.ReadLine();
                if (rawAnswer == null)
                {
                    return false;
                }

                var normalizedAnswer = rawAnswer.Trim();
                if (normalizedAnswer.Equals("y", StringComparison.OrdinalIgnoreCase)
                    || normalizedAnswer.Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (normalizedAnswer.Equals("n", StringComparison.OrdinalIgnoreCase)
                    || normalizedAnswer.Equals("no", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                _console.WriteLine("Error: Please answer y/n.");
            }
        }

        public bool AskYesNoDefaultYes(string prompt)
        {
            while (true)
            {
                _console.Write($"{prompt} ");

                var rawAnswer = _console.ReadLine();
                if (rawAnswer == null)
                {
                    return false;
                }

                var normalizedAnswer = rawAnswer.Trim();
                if (normalizedAnswer.Length == 0)
                {
                    return true;
                }

                if (normalizedAnswer.Equals("y", StringComparison.OrdinalIgnoreCase)
                    || normalizedAnswer.Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (normalizedAnswer.Equals("n", StringComparison.OrdinalIgnoreCase)
                    || normalizedAnswer.Equals("no", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                _console.WriteLine("Error: Please answer y/n.");
            }
        }

        public bool AskYesNoDefaultNo(string prompt)
        {
            while (true)
            {
                _console.Write($"{prompt} ");

                var rawAnswer = _console.ReadLine();
                if (rawAnswer == null)
                {
                    return false;
                }

                var normalizedAnswer = rawAnswer.Trim();
                if (normalizedAnswer.Length == 0)
                {
                    return false;
                }

                if (normalizedAnswer.Equals("y", StringComparison.OrdinalIgnoreCase)
                    || normalizedAnswer.Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (normalizedAnswer.Equals("n", StringComparison.OrdinalIgnoreCase)
                    || normalizedAnswer.Equals("no", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                _console.WriteLine("Error: Please answer y/n.");
            }
        }

        public bool AskYesNoWithCancel(string prompt, out bool isCanceled)
        {
            isCanceled = false;

            while (true)
            {
                _console.Write($"{prompt} ");

                var rawAnswer = _console.ReadLine();
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

                if (normalizedAnswer.Equals("y", StringComparison.OrdinalIgnoreCase)
                    || normalizedAnswer.Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (normalizedAnswer.Equals("n", StringComparison.OrdinalIgnoreCase)
                    || normalizedAnswer.Equals("no", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                _console.WriteLine("Error: Please answer y/n.");
            }
        }
    }
}
