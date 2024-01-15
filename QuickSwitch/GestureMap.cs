using Microsoft.VisualStudio.RpcContracts.Commands;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace QuickSwitch
{
    internal static class GestureMap
    {
        private static readonly List<string> _commandNames = new List<string>()
        {
            SwitchCommand.CommandFullName
        };

        private static readonly Dictionary<string, List<KeyGesture>> _map = new Dictionary<string, List<KeyGesture>>();

        public static void Update(EnvDTE.DTE dte)
        {
            _map.Clear();

            // get the key binding setting
            foreach (EnvDTE.Command command in dte.Commands)
            {
                if (!_commandNames.Contains(command.Name))
                    continue;
                var bindings = (object[])command.Bindings;
                if (bindings.Length == 0)
                    break;

                foreach (string binding in bindings)
                {
                    // trim the Global:: prefix
                    var keys = binding.Replace("Global::", "");
                    var gesture = (KeyGesture)new KeyGestureConverter().ConvertFromString(keys);
                    if (_map.TryGetValue(command.Name, out var gestures))
                        gestures.Add(gesture);
                    else
                        _map.Add(command.Name, new List<KeyGesture>() { gesture });
                }
            }
        }

        public static IEnumerable<KeyGesture> GetGestures(string commandName)
        {
            if (_map.TryGetValue(commandName, out var gestures))
                return gestures;
            return Enumerable.Empty<KeyGesture>();
        }

    }
}
