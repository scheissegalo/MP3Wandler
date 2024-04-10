using Gtk;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Application = Gtk.Application;

namespace FiletypeConverter
{
	class Program
	{
		static void Main(string[] args)
		{
			Application.Init();
			var win = new Window("MP3 Converter v1.0 - Progress")
			{
				DefaultWidth = 400,
				DefaultHeight = 200 // Adjust the window height as needed
			};
			//win.SetIconFromFile("appicon.png");
			//win.DeleteEvent += (o, args) => Application.Quit();

			win.DeleteEvent += (o, args) => Application.Quit();

			// Use Box with vertical orientation instead of VBox
			var vbox = new Box(Orientation.Vertical, spacing: 6);
			win.Add(vbox);

			var progressBar = new ProgressBar();
			progressBar.SetSizeRequest(width: 400, height: 50); // Correct way to set size

			var statusLabel = new Label
			{
				Text = "Initializing..."
			};

			// Adding a Spinner
			var spinner = new Spinner();

			vbox.PackStart(progressBar, expand: false, fill: false, padding: 0);
			vbox.PackStart(statusLabel, expand: false, fill: false, padding: 0);
			vbox.PackStart(spinner, expand: false, fill: false, padding: 5); // Add some padding for visual separation

			win.ShowAll();
			spinner.Start(); // Start the spinner animation

			// Run the conversion process in a background task to keep the GUI responsive
			Task.Run(() => ConvertFiles(progressBar, statusLabel, spinner));

			Application.Run();
		}

		static void ConvertFiles(ProgressBar progressBar, Label statusLabel, Spinner spinner)
		{
			string directoryPath = Directory.GetCurrentDirectory();
			string outputDirectoryPath = Path.Combine(directoryPath, "MP3");
			string[] fileTypes = { "*.mp4", "*.webm", "*.m4a" };
			int totalFiles = fileTypes.Sum(ext => Directory.EnumerateFiles(directoryPath, ext, SearchOption.AllDirectories).Count());
			int convertedFiles = 0;

			if (!Directory.Exists(outputDirectoryPath))
			{
				Directory.CreateDirectory(outputDirectoryPath);
			}

			foreach (string fileType in fileTypes)
			{
				var files = Directory.EnumerateFiles(directoryPath, fileType, SearchOption.AllDirectories);

				foreach (var file in files)
				{
					// Use Regex to remove text within parentheses
					string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
					string cleanedFileName = Regex.Replace(fileNameWithoutExtension, @"\s*\([^)]*\)", "").Trim();

					string outputFileName = Path.Combine(outputDirectoryPath, cleanedFileName + ".mp3");
					//string outputFileName = Path.Combine(outputDirectoryPath, Path.GetFileNameWithoutExtension(file) + ".mp3");

					if (!File.Exists(outputFileName))
					{
						Application.Invoke(delegate
						{
							statusLabel.Text = $"Converting: {Path.GetFileName(file)}";
						});
						ConvertToMp3(file, outputFileName);
						convertedFiles++;
					}

					Application.Invoke(delegate
					{

						progressBar.Fraction = (double)convertedFiles / totalFiles;
						progressBar.Text = $"{convertedFiles} of {totalFiles} files converted";
					});
				}
			}

			Application.Invoke(delegate
			{
				spinner.Stop(); // Stop the spinner animation
				statusLabel.Text = $"Conversion complete! {totalFiles} of {totalFiles} files converted";
			});
		}

		static void ConvertToMp3(string inputFile, string outputFile)
		{
			var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "ffmpeg",
					Arguments = $"-i \"{EscapeCommandLineArgument(inputFile)}\" \"{EscapeCommandLineArgument(outputFile)}\"",
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true,
				}
			};

			process.Start();
			process.WaitForExit();
		}

		static string EscapeCommandLineArgument(string argument)
		{
			return argument.Replace("\"", "\\\"");
		}
	}
}
