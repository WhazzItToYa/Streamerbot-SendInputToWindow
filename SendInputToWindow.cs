using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Text;

public class CPHInline
{
    // P/Invoke declarations
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    // Delegate for EnumWindows
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    
    // Extended dictionary of all Windows Virtual-Key Codes
    Dictionary<string, int> VirtualKeyCodes = new Dictionary<string, int>
    {
        // Mouse buttons
        { "LBUTTON", 0x01 }, // Left mouse button
        { "RBUTTON", 0x02 }, // Right mouse button
        { "MBUTTON", 0x04 }, // Middle mouse button
        { "XBUTTON1", 0x05 }, // X1 mouse button
        { "XBUTTON2", 0x06 }, // X2 mouse button
        // Control keys
        { "BACK", 0x08 }, // Backspace key
        { "TAB", 0x09 },  // Tab key
        { "RETURN", 0x0D }, // Enter key
        { "ENTER", 0x0D }, // Enter key
        { "SHIFT", 0x10 }, // Shift key
        { "CONTROL", 0x11 }, // Ctrl key
        { "CTRL", 0x11 }, // Ctrl key
        { "ALT", 0x12 }, // Alt key
        { "PAUSE", 0x13 }, // Pause key
        { "CAPSLOCK", 0x14 }, // Caps Lock key
        // Navigation and editing
        { "ESCAPE", 0x1B }, // Escape key
        { "SPACE", 0x20 }, // Spacebar
        { " ", 0x20 }, // Spacebar
        { "PAGEUP", 0x21 }, // Page Up key
        { "PAGEDOWN", 0x22 }, // Page Down key
        { "END", 0x23 }, // End key
        { "HOME", 0x24 }, // Home key
        { "LEFT", 0x25 }, // Left arrow key
        { "UP", 0x26 }, // Up arrow key
        { "RIGHT", 0x27 }, // Right arrow key
        { "DOWN", 0x28 }, // Down arrow key
        { "SELECT", 0x29 }, // Select key
        { "PRINT", 0x2A }, // Print key
        { "EXECUTE", 0x2B }, // Execute key
        { "PRINTSCREEN", 0x2C }, // Print Screen key
        { "INSERT", 0x2D }, // Insert key
        { "DELETE", 0x2E }, // Delete key
        { "HELP", 0x2F }, // Help key
        // Number keys (0-9)
        { "0", 0x30 },
        { "1", 0x31 },
        { "2", 0x32 },
        { "3", 0x33 },
        { "4", 0x34 },
        { "5", 0x35 },
        { "6", 0x36 },
        { "7", 0x37 },
        { "8", 0x38 },
        { "9", 0x39 },
        // Letter keys (A-Z)
        { "A", 0x41 },
        { "B", 0x42 },
        { "C", 0x43 },
        { "D", 0x44 },
        { "E", 0x45 },
        { "F", 0x46 },
        { "G", 0x47 },
        { "H", 0x48 },
        { "I", 0x49 },
        { "J", 0x4A },
        { "K", 0x4B },
        { "L", 0x4C },
        { "M", 0x4D },
        { "N", 0x4E },
        { "O", 0x4F },
        { "P", 0x50 },
        { "Q", 0x51 },
        { "R", 0x52 },
        { "S", 0x53 },
        { "T", 0x54 },
        { "U", 0x55 },
        { "V", 0x56 },
        { "W", 0x57 },
        { "X", 0x58 },
        { "Y", 0x59 },
        { "Z", 0x5A },
        // Function keys (F1-F24)
        { "F1", 0x70 },
        { "F2", 0x71 },
        { "F3", 0x72 },
        { "F4", 0x73 },
        { "F5", 0x74 },
        { "F6", 0x75 },
        { "F7", 0x76 },
        { "F8", 0x77 },
        { "F9", 0x78 },
        { "F10", 0x79 },
        { "F11", 0x7A },
        { "F12", 0x7B },
        { "F13", 0x7C },
        { "F14", 0x7D },
        { "F15", 0x7E },
        { "F16", 0x7F },
        { "F17", 0x80 },
        { "F18", 0x81 },
        { "F19", 0x82 },
        { "F20", 0x83 },
        { "F21", 0x84 },
        { "F22", 0x85 },
        { "F23", 0x86 },
        { "F24", 0x87 },
        // Numpad keys
        { "NUMLOCK", 0x90 },
        { "SCROLLLOCK", 0x91 },
        { "NUMPAD0", 0x60 },
        { "NUMPAD1", 0x61 },
        { "NUMPAD2", 0x62 },
        { "NUMPAD3", 0x63 },
        { "NUMPAD4", 0x64 },
        { "NUMPAD5", 0x65 },
        { "NUMPAD6", 0x66 },
        { "NUMPAD7", 0x67 },
        { "NUMPAD8", 0x68 },
        { "NUMPAD9", 0x69 },
        { "MULTIPLY", 0x6A },
        { "ADD", 0x6B },
        { "SEPARATOR", 0x6C },
        { "SUBTRACT", 0x6D },
        { "DECIMAL", 0x6E },
        { "DIVIDE", 0x6F },
        // Left/Right variants, app-specific control keys
        { "LSHIFT", 0xA0 },
        { "RSHIFT", 0xA1 },
        { "LCONTROL", 0xA2 },
        { "LCTRL", 0xA2 },
        { "RCONTROL", 0xA3 },
        { "RCTRL", 0xA3 },
        { "LMENU", 0xA4 },
        { "RMENU", 0xA5 },
        { "BROWSER_BACK", 0xA6 },
        { "BROWSER_FORWARD", 0xA7 },
        { "BROWSER_REFRESH", 0xA8 },
        { "BROWSER_STOP", 0xA9 },
        { "BROWSER_SEARCH", 0xAA },
        { "BROWSER_FAVORITES", 0xAB },
        { "BROWSER_HOME", 0xAC },
        { "VOLUME_MUTE", 0xAD },
        { "VOLUME_DOWN", 0xAE },
        { "VOLUME_UP", 0xAF },
        { "MEDIA_NEXT_TRACK", 0xB0 },
        { "MEDIA_PREV_TRACK", 0xB1 },
        { "MEDIA_STOP", 0xB2 },
        { "MEDIA_PLAY_PAUSE", 0xB3 },
        { "LAUNCH_MAIL", 0xB4 },
        { "LAUNCH_MEDIA_SELECT", 0xB5 },
        { "LAUNCH_APP1", 0xB6 },
        { "LAUNCH_APP2", 0xB7 },
        // EOM keys that can vary by manufacturer, but have common US mappings.
        // Sorry world.
        { ";", 0xBA },
        { ":", 0xBA },
        { "+", 0XBB },
        { "=", 0XBB },
        { ",", 0xBC },
        { "<", 0xBC },
        { "-", 0xBD },
        { "_", 0xBD },
        { ".", 0xBE },
        { ">", 0xBE },
        { "/", 0xBF },
        { "?", 0xBF },
        { "~", 0xC0 },
        { "`", 0xC0 },
        { "[", 0xDB},
        { "\\", 0xDC},
        { "|", 0xDC},
        { "]", 0xDD},
        { "'", 0xDE},
        { "\"", 0xDE},
    };
    
    private const uint WM_KEYDOWN = 0x0100;
    private const uint WM_KEYUP = 0x0101;
    
    public bool Execute()
    {
        CPH.TryGetArg("verboseLogging", out verbose);
        
        if (!CPH.TryGetArg("keypresses", out string keypresses))
        {
            ERROR("Misssing required argument \"keypresses\"");
            return false;
        }

        CPH.TryGetArg("keypressDelay", out int keypressDelay);
        CPH.TryGetArg("keypressHold", out int keypressHold);
        
        IntPtr hwnd = MatchWindow();
        List<Action> actions = GenerateKeyActions(keypresses, hwnd, keypressDelay, keypressHold);
        foreach (Action action in actions)
        {
            action();
        }

        return true;
    }

    // Converts a string containing a sequence of KEYNAME,
    // and converts it to an array of functions to call in sequence to
    // execute the keypresses and other actions.
    //
    // Each KEYNAME is a case-insensitive name of a key, and can either be:
    //   * a single character denoting a regular key, e.g., "a" or "1"
    //   * OR  "{NAME[:HOLD]}", where NAME is one of the named keys listed in the table above,
    //           and HOLD is an optional number of milliseconds to hold the key down.

    private List<Action> GenerateKeyActions(string keypresses, IntPtr hwnd, int keypressDelay=0, int keypressHold=0)
    {
        string upkeys = keypresses.ToUpper();

        // Extract all of the individual key characters and {...} directives.
        MatchCollection matches = Regex.Matches(upkeys, "[^{]|{[^}]*}", RegexOptions.None);

        // Decode each parsed element into actions
        List<Action> actions = new List<Action>();
        foreach (Match match in matches)
        {
            int hold = keypressHold;
            
            // Strip off any surrounding {} and use the contents as the name of the key to be looked up.
            string keyname = match.Value.TrimStart('{').TrimEnd('}');

            // {KEY:NUMBER} means to hold the key down for NUMBER ms.
            int pos = keyname.IndexOf(':', 1);
            if (pos > 0)
            {
                int.TryParse(keyname.Substring(pos+1), out hold);
                keyname = keyname.Substring(0, pos);
            }
            
            if (VirtualKeyCodes.TryGetValue(keyname, out int keycode))
            {
                // Key down
                actions.Add(() => {SendMessage(hwnd, WM_KEYDOWN, (IntPtr)keycode, IntPtr.Zero);});

                // Optional key down hold time
                if (keypressHold > 0) actions.Add(() => {CPH.Wait(hold);});

                // Key up
                actions.Add(() => {SendMessage(hwnd, WM_KEYUP, (IntPtr)keycode, IntPtr.Zero);});
                
                // Optional between-key pause
                if (keypressDelay > 0) actions.Add(() => {CPH.Wait(keypressDelay);});
            }
            else
            {
                WARN($"\"{match.Value}\" is not a valid virtual key name.");
            }
        }
        return actions;
    }

    // Finds a window matching various window & process criteria
    private IntPtr MatchWindow()
    {
        bool anyCriteria = false;
        anyCriteria |= CPH.TryGetArg("targetWindowTitle", out string targWinTitle);
        anyCriteria |= CPH.TryGetArg("targetProcess", out string targProcName);
        anyCriteria |= CPH.TryGetArg("targetExecutable", out string targExePath);
        anyCriteria |= CPH.TryGetArg("targetWindowClass", out string targWinClass);

        DEBUG($"Criteria: c:\"{targWinClass}\" pn:\"{targProcName}\" pp:\"{targExePath}\" t:\"{targWinTitle}\"");

        if (!anyCriteria)
        {
            ERROR("You must supply at least one argument of targetWindowTitle, targetWindowClass, targetExecutable, or targetProcess");
            return IntPtr.Zero;
        }
        
        IntPtr bestMatch = IntPtr.Zero;

        // Store the results of matches
        IntPtr matchedHandle = IntPtr.Zero;
        int bestMatchScore = int.MinValue;

        EnumWindows((hWnd, lParam) =>
        {
            // Get window title
            StringBuilder windowTitle = new StringBuilder(256);
            GetWindowText(hWnd, windowTitle, windowTitle.Capacity);

            // Get class name
            StringBuilder className = new StringBuilder(256);
            GetClassName(hWnd, className, className.Capacity);
            
            VERBOSE($"Matching {hWnd}: c:\"{className}\" t:\"{windowTitle}\"");
            
            // Scoring for matching
            int matchScore = 500;

            // Window class must match exactly, or it is disqualified
            if (!string.IsNullOrEmpty(targWinClass) && !className.ToString().Equals(targWinClass, StringComparison.OrdinalIgnoreCase))
            {
                VERBOSE("    Class doesn't match at all");
                return true; // skip
            }

            // Window title must match at least part of title, score decreased by the edit distance.
            if (!string.IsNullOrEmpty(targWinTitle))
            {
                if (windowTitle.ToString().IndexOf(targWinTitle, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    matchScore += (targWinTitle.Length - windowTitle.Length);
                }
                else
                {
                    VERBOSE("    Title doesn't match at all");
                    return true; // skip
                }
            }


            // Fetching process info is relatively expensive, so only do it
            // after filtering for window info, and if it's part of the match criteria

            if (!string.IsNullOrEmpty(targExePath) || !string.IsNullOrEmpty(targProcName))
            {
                // Get process ID and executable
                GetWindowThreadProcessId(hWnd, out uint processId);
                (string processExecutable, string processName) = GetProcessInfo((int)processId);
                VERBOSE($"{hWnd}: c:\"{className}\" t:\"{windowTitle}\" pn:\"{processName}\" pp:\"{processExecutable}\"");
            
                // Exe and Path are absolute criteria.  If it doesn't match those, then it's out of the running.
                if (!string.IsNullOrEmpty(targExePath) && processExecutable != null)
                {
                    // Full path
                    if (processExecutable.Equals(targExePath, StringComparison.OrdinalIgnoreCase))
                    {
                        matchScore += 1000;
                    }
                    // file name only
                    else if (Path.GetFileName(processExecutable).Equals(targExePath, StringComparison.OrdinalIgnoreCase))
                    {
                        matchScore += 500;
                    }
                    else
                    {
                        VERBOSE("    Path doesn't match at all");
                        return true; // skip to next window
                    }
                }
                
                // Process name must match at least partially, score decreased by the edit distance.
                if (!string.IsNullOrEmpty(targProcName) && !string.IsNullOrEmpty(processName))
                {
                    if (processName.IndexOf(targProcName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        matchScore += (targProcName.Length - processName.Length);
                    }
                    else
                    {
                        VERBOSE("    Process name doesn't match at all");
                        return true; // skip
                    }
                }
            }

            VERBOSE($"  Score: {matchScore} window: {windowTitle}");
            
            // Check if this is the best match so far
            if (matchScore > bestMatchScore)
            {
                DEBUG($"Best Match so far: score:{matchScore} w:{hWnd}: c:\"{className}\" t:\"{windowTitle}\"");
                bestMatchScore = matchScore;
                matchedHandle = hWnd;
            }

            return true; // Continue enumeration
        }, IntPtr.Zero);

        return matchedHandle;
    }

    private Dictionary<int, (string, string)> ProcessCache = new Dictionary<int, (string, string)>();
    
    private (string executable, string name) GetProcessInfo(int processId)
    {
        if (!ProcessCache.ContainsKey(processId))
        {
            try
            {
                VERBOSE($"Fetching fresh process info for {processId}");
                Process process = Process.GetProcessById(processId);
                ProcessCache[processId] = (process.MainModule.FileName, process.ProcessName);
            }
            catch
            {
                ProcessCache[processId] = (null, null);
            }
        }
        return ProcessCache[processId];
    }

    
    private void DEBUG(string msg)
    {
        CPH.LogDebug($"SENDAPPKEY: {msg}");
    }

    private bool verbose = false;
    
    private void VERBOSE(string msg)
    {
        if (verbose) CPH.LogVerbose($"SENDAPPKEY: {msg}");
    }
    private void WARN(string msg)
    {
        CPH.LogWarn($"SENDAPPKEY: {msg}");
    }
    private void ERROR(string msg)
    {
        CPH.LogError($"SENDAPPKEY: {msg}");
    }

}
