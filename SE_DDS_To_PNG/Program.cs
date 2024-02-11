using System.Diagnostics;

namespace SE_DDS_To_PNG
{
    internal class Program
    {
        static string TexConvPath = "";
        static List<string> Errors = [];

        static void Main(string[] args)
        {
            string BaseTitle = "Space Engineers DDS Converter";
            Console.Title = BaseTitle;
            Console.CursorVisible = false;
            if (File.Exists("C:\\Program Files (x86)\\Steam\\steamapps\\common\\SpaceEngineersModSDK\\Tools\\TexturePacking\\Tools\\texconv.exe"))
            {
                TexConvPath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\SpaceEngineersModSDK\\Tools\\TexturePacking\\Tools\\texconv.exe";
            }
            else
            {
                try
                {
                    Process.Start("texconv").Kill();
                    TexConvPath = "textconv";
                }
                catch
                {
                    Console.Title = BaseTitle + " - Error";
                    Console.WriteLine("Error: You need to set texconv.exe to your PATH system variable.\n" +
                        "texconv.exe is at \"\\SpaceEngineersModSDK\\Tools\\TexturePacking\\Tools\"");
                    Console.ReadKey(true);
                    return;
                }
            }

            string[] Files;

            string[] Formats =
            [
                "dds",
                "png",
                "tif",
                "jpg"
            ];

            Console.Title = BaseTitle + " - Select Format";
            string Format = Formats[Menu("Please select the output image format:", Formats)];
            Console.Title = BaseTitle + $" - Converting To {Format.ToUpper()}";
            string[] OtherFormats = GetOtherFormats(Formats, Format);

            if (args.Length == 0)
            {
                Files = [.. GetFiles(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), OtherFormats)];
            }
            else
            {
                List<string> TmpFiles = [];
                foreach (string arg in args)
                {
                    if (Directory.Exists(arg))
                    {
                        TmpFiles.AddRange(GetFiles(arg, OtherFormats));
                    }
                    else if (SelectFile(arg, OtherFormats))
                    {
                        TmpFiles.Add(arg);
                    }
                }
                Files = [.. TmpFiles];
            }

            int Total = Files.Length;
            int Current = 0;
            long StartTime = DateTime.Now.Ticks;
            int CurrentRunning = 0;

            using Task PrintTask = Task.Run(() =>
            {
                if (Format == "dds")
                {
                    Console.WriteLine("Converting...\n" +
                        "[.png  => .dds]\n" +
                        "[.tif  => .dds]\n" +
                        "[.jpg  => .dds]\n" +
                        "[.jpeg => .dds]");
                }
                else
                {
                    Console.WriteLine("Converting...\n" +
                        $"[.dds => .{Format}]");
                }

                while (true)
                {
                    Console.SetCursorPosition(0, 6);

                    float Percentage = (float)Math.Round((float)Current / Total * 10000f) / 100f;
                    if (float.IsNaN(Percentage)) Percentage = 0f;

                    Console.WriteLine($"{Current} / {Total} converted. ({Percentage}%)     ");
                    Console.WriteLine($"Currently converting: {CurrentRunning - Current}");

                    Console.Title = BaseTitle + $" - Converting To {Format} ({Percentage}%)";

                    string TimeLeftStr = "Loading...";
                    string ImgSecStr = "Loading...";

                    long TimeLeft = 0L;
                    long CurrentTime = (DateTime.Now.Ticks - StartTime);
                    if (Current > 10)
                    {
                        TimeLeft = CurrentTime / Current * (Total - Current);
                        TimeLeftStr = TimeToString(TimeLeft);
                        ImgSecStr = (Math.Round(new TimeSpan(Current / (CurrentTime - StartTime) * 10L).TotalSeconds) / 10d).ToString();
                    }

                    if (Total <= 10 || new TimeSpan(TimeLeft).TotalSeconds < 5d)
                    {
                        TimeLeftStr = "A few seconds.";
                    }

                    Console.WriteLine($"Time left: {TimeLeftStr}     ");
                    Console.WriteLine($"Images per second: {ImgSecStr}     \n");

                    if (Errors.Count == 0)
                        Console.ForegroundColor = ConsoleColor.Green;
                    else
                        Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Errors: {Errors.Count}     \n");

                    foreach (string Error in Errors)
                    {
                        Console.WriteLine(Error + '\n');
                    }
                    Console.ForegroundColor = ConsoleColor.White;

                    Thread.Sleep(500);
                }
            });

            if (Format == "dds")
            {
                Parallel.ForEach(Files, (File) =>
                {
                    CurrentRunning++;
                    ConvertToDDS(File);
                    Current++;
                });
            }
            else
            {
                Parallel.ForEach(Files, (File) =>
                {
                    CurrentRunning++;
                    ConvertTo(File, Format);
                    Current++;
                });
            }

            Process.GetCurrentProcess().Kill();
        }

        static List<string> GetFiles(string Dir, string[] OtherFormat)
        {
            List<string> Files = [.. Directory.GetFiles(Dir)];

            Files.RemoveAll(x => !SelectFile(x, OtherFormat));

            foreach (string SubDir in Directory.GetDirectories(Dir))
            {
                Files.AddRange(GetFiles(SubDir, OtherFormat));
            }

            return Files;
        }
        static void ConvertTo(string File, string Format)
        {
            string OutputPath = Path.ChangeExtension(File, $".{Format}");
            if (!System.IO.File.Exists(OutputPath))
            {
                Process P = Process.Start($"\"{TexConvPath}\" -ft {Format.ToUpper()} -if LINEAR -sRGB -o \"{Path.GetDirectoryName(OutputPath)}\" \"{File}\"");
                P.WaitForExit(10000);
                string Output = P.StandardOutput.ReadToEnd();
                if (Output.Contains("FAILED"))
                {
                    Errors.Add(Output);
                }
                else
                {
                    System.IO.File.Move(Path.ChangeExtension(OutputPath, $".{Format.ToUpper()}"), OutputPath);
                }
            }
        }
        static void ConvertToDDS(string File)
        {
            string OutputPath = Path.ChangeExtension(File, ".dds");
            if (!System.IO.File.Exists(OutputPath))
            {
                Process P = Process.Start($"\"{TexConvPath}\" -f BC7_UNORM -if LINEAR -sRGB -o \"{Path.GetDirectoryName(OutputPath)}\" \"{File}\"");
                P.WaitForExit(10000);
                string Output = P.StandardOutput.ReadToEnd();
                if (Output.Contains("FAILED"))
                {
                    Errors.Add(Output);
                }
                else
                {
                    System.IO.File.Move(Path.ChangeExtension(OutputPath, ".DDS"), OutputPath);
                }
            }
        }

        static string TimeToString(long Ticks)
        {
            DateTime T = new(Ticks);
            int Min = T.Minute;
            int Sec = T.Second;
            string Out = "";

            if (Min < 10)
            {
                Out += '0';
            }
            Out += Min;

            if (Sec < 10)
            {
                Out += '0';
            }
            Out += Sec;

            return Out;
        }

        static int Menu(string Headline, string[] Options)
        {
            int Current = 0;
            while (true)
            {
                Console.SetCursorPosition(0, 0);
                Console.WriteLine(Headline);
                for (int i = 0; i < Options.Length; i++)
                {
                    if (i == Current)
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.White;
                        Console.WriteLine($"[{i + 1}] => {Options[i]}");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.BackgroundColor = ConsoleColor.Black;
                    }
                    else
                    {
                        Console.WriteLine($"[{i + 1}] => {Options[i]}");
                    }
                }

                ConsoleKeyInfo Key = Console.ReadKey(true);

                if (int.TryParse(Key.KeyChar.ToString(), out int Tmp))
                {
                    Current = Tmp - 1;
                }

                switch (Key.Key)
                {
                    case ConsoleKey.DownArrow:
                        {
                            Current++;
                            break;
                        }
                    case ConsoleKey.UpArrow:
                        {
                            Current--;
                            break;
                        }
                    case ConsoleKey.Enter:
                    case ConsoleKey.Escape:
                        {
                            return Current;
                        }
                }

                if (Current < 0)
                    Current = 0;
                else if (Current >= Options.Length)
                    Current = Options.Length - 1;
            }
        }

        static string[] GetOtherFormats(string[] Formats, string Format)
        {
            if (Format == "dds")
            {
                List<string> Tmp = [.. Formats];
                Tmp.Remove("dds");
                return [.. Tmp];
            }
            else
            {
                return ["dds"];
            }
        }
        static bool SelectFile(string FilePath, string[] OtherFormats)
        {
            if (!File.Exists(FilePath))
                return false;

            string Lower = FilePath.ToLower();
            foreach (string Format in OtherFormats)
            {
                if (Lower.EndsWith($".{Format}"))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
