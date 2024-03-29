# QuickSwitch

An extension for Visual Studio for quickly switching between similar files.

Works just like Ctrl+Tab. Press Ctrl+Alt+O to open the window. Hold Alt while pressing O to pick the file. Release Alt to select the file.

To change the shortcut, go to Tools -> Options -> Environment -> Keyboard and search for `Tools.InvokeSwitchCommand`. Make sure there are no conflicts with other commands or it won't work.

Some example use cases:

Switch between header and source files

![cpp_example.png](https://i.imgur.com/wwsDPhA.png)

Switch between XAML and code-behind

![xaml_example.png](https://i.imgur.com/kDqIMYv.png)

Switch between View and ViewModel

![image.png](https://i.imgur.com/90BGKtH.png)