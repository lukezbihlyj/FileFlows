// DISABLE Since rar can be a PITA to get working in the docker test container
// #if(DEBUG)
//
// using System.Diagnostics;
// using System.IO;
// using FileFlows.FlowRunner.Helpers;
//
// namespace FileFlowsTests.Tests.RunnerTests.ArchiveHelperTests;
//
// /// <summary>
// /// Tests RAR files
// /// </summary>
// [TestClass]
// public class RarTests : TestBase
// {
//     /// <summary>
//     /// Tests a multipart rar file can be extracted
//     /// </summary>
//     [TestMethod]
//     public void MutlipartRarTest_Internal()
//     {
//         string rarDirectory = $"{TempPath}/{Guid.NewGuid()}";
//         Directory.CreateDirectory(rarDirectory);
//         string rarFile = CreateMultipartRarFile(rarDirectory);
//
//         // Check that multiple RAR files are created
//         string[] rarParts = Directory.GetFiles(rarDirectory, "*.rar*");
//         Assert.IsTrue(rarParts.Length > 1, $"Expected multiple RAR parts, but found {rarParts.Length}.");
//
//         var helper = new ArchiveHelper(Logger, "rar", "unrar", "7z");
//         var result = helper.ExtractMultipartRar(rarFile, TempPath);
//         if (result.Failed(out string error))
//             Assert.Fail(error);
//         Assert.IsTrue(result.Value);
//
//         // Cleanup the generated test files
//         Directory.Delete(rarDirectory, true);
//     }
//
//     /// <summary>
//     /// Creates a multipart RAR file for testing
//     /// </summary>
//     /// <param name="directory">The directory to create the RAR file in</param>
//     /// <returns>The path to the first RAR file</returns>
//     private string CreateMultipartRarFile(string directory)
//     {
//         string rarFileBase = Path.Combine(directory, "multi-part-rar");
//         string rarFile = rarFileBase + ".rar";
//
//         // Create a large test file by writing random data
//         string testFile1 = Path.Combine(directory, "test1.bin");
//         using (var fs = new FileStream(testFile1, FileMode.Create, FileAccess.Write, FileShare.None))
//         {
//             byte[] data = new byte[1024 * 1024]; // 1 MB buffer
//             var rng = new Random();
//         
//             for (int i = 0; i < 10; i++) // Write 10 MB total
//             {
//                 rng.NextBytes(data); // Fill buffer with random data
//                 fs.Write(data, 0, data.Length);
//             }
//         }
//
//         string testFile2 = Path.Combine(directory, "test2.bin");
//         File.Copy(testFile1, testFile2);
//
//         // Use 7z to create a multipart RAR file (splitting at 5 MB)
//         var startInfo = new ProcessStartInfo
//         {
//             FileName = "rar",
//             Arguments = $"a -v5m \"{rarFile}\" \"{testFile1}\" \"{testFile2}\"",
//             RedirectStandardOutput = true,
//             RedirectStandardError = true,
//             UseShellExecute = false,
//             CreateNoWindow = true
//         };
//
//         using (var process = Process.Start(startInfo))
//         {
//             string output = process.StandardOutput.ReadToEnd();
//             string error = process.StandardError.ReadToEnd();
//             process.WaitForExit();
//
//             if (process.ExitCode != 0)
//             {
//                 throw new Exception($"Failed to create multipart RAR file. Output: {output}. Error: {error}");
//             }
//         }
//
//         return rarFile;
//     }
// }
//
// #endif