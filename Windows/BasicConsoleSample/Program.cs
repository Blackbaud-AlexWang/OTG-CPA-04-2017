// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// 
// Microsoft Cognitive Services: http://www.microsoft.com/cognitive
// 
// Microsoft Cognitive Services Github:
// https://github.com/Microsoft/Cognitive
// 
// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 

using System;
using VideoFrameAnalyzer;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace BasicConsoleSample
{
    internal class Program
    {
        private static FaceServiceClient faceClient { get; set; }

        private static void NicelyExit(string message, bool pause = true)
        {
            Console.WriteLine(message);
            Console.WriteLine("Press enter to exit...");
            if (pause)
            {
                Console.ReadLine();
            }
            Environment.Exit(Environment.ExitCode);
        }

        private static void PrintError(string message)
        {
            Console.WriteLine($"ERROR: {message}");
        }

        private static void CreatePersonGroup()
        {
            Console.WriteLine("Enter personGroupId (type 'return' to exit to main menu):");
            string personGroupId = Console.ReadLine();

            if (personGroupId.Equals("return", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Console.WriteLine("Enter personGroupName:");
            string personGroupName = Console.ReadLine();

            Console.WriteLine($"Attempting to create person group '{personGroupId}' as '{personGroupName}'...");

            try
            {
                faceClient.CreatePersonGroupAsync(personGroupId, personGroupName).Wait(6000);
            }
            catch(Exception e)
            {
                PrintError(e.Message);
                return;
            }
            Console.WriteLine($"Successfully created person group '{personGroupId}' as '{personGroupName}'!");
            Console.WriteLine("");
        }

        public async static Task CreatePerson()
        {
            Console.WriteLine("Enter personGroupId (type 'return' to exit to main menu):");
            string personGroupId = Console.ReadLine();

            if (personGroupId.Equals("return", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Console.WriteLine("Enter name of person:");
            string personName = Console.ReadLine();

            Console.WriteLine($"Attempting to create person '{personName}' in '{personGroupId}'...");

            try
            {
                await faceClient.CreatePersonAsync(personGroupId, personName);
            }
            catch (Exception e)
            {
                PrintError(e.Message);
                return;
            }
            Console.WriteLine($"Successfully created person '{personName}' in '{personGroupId}'!");
            Console.WriteLine("");
        }

        private async static Task ShowAllPeople()
        {
            Console.WriteLine("Person group you want to see (type 'return' to exit to main menu):");

            string personGroupId = Console.ReadLine();

            if (personGroupId.Equals("return", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Console.WriteLine($"Gathering people for '{personGroupId}' group...");
            Person[] people = await faceClient.GetPersonsAsync(personGroupId);
            Console.WriteLine($"<pname>: <pid>");
            foreach (var p in people)
            {
                Console.WriteLine($"{p.Name}: {p.PersonId}");
            }
            Console.WriteLine("");
        }

        private async static Task ShowAllPersonGroups()
        {
            Console.WriteLine("Gathering person groups...");
            PersonGroup[] groups = await faceClient.GetPersonGroupsAsync();
            Console.WriteLine("<pgname>: <pgid> <userdata>");
            foreach (var g in groups)
            {
                Console.WriteLine($"{g.Name}: {g.PersonGroupId} {g.UserData}");
            }
            Console.WriteLine("");
        }

        private async static Task UploadPhotosAndTrainProgram()
        {
            Console.WriteLine("NOTICE: This method may take some time. If you wish to continue type 'y'.");
            if (!Console.ReadLine().Equals("y"))
            {
                return;
            }

            Console.WriteLine("Please specify personGroupId of user:");
            string personGroupId = Console.ReadLine();
            Console.WriteLine("Please specify name of user:");
            string userName = Console.ReadLine();
            Console.WriteLine("Please specify path to training photos directory:");
            string path = Console.ReadLine();
            Console.WriteLine("");
            Console.WriteLine("Gettings user information...");
            Person[] people = await faceClient.GetPersonsAsync(personGroupId);
            Person user = people.OfType<Person>().ToList().First<Person>(p => p.Name.Equals(userName, StringComparison.OrdinalIgnoreCase));
            Console.WriteLine($"Found user {user.Name} as {user.PersonId}!");
            Console.WriteLine("Attempting to upload photos...");
            foreach (string imagePath in Directory.GetFiles(path, "*.jpg"))
            {
                using (Stream s = File.OpenRead(imagePath))
                {
                    faceClient.AddPersonFaceAsync(personGroupId, user.PersonId, s).Wait(6000000);
                }
            }
            Console.WriteLine("Photos uploaded!");
            Console.WriteLine("Attempting to train group... Please check status from main menu");
            await faceClient.TrainPersonGroupAsync(personGroupId);
            Console.WriteLine("");
        }

        private async static Task CheckIfGroupIsStillTraining()
        {
            Console.WriteLine("Person group you want to see if still running (type 'return' to exit to main menu):");

            string personGroupId = Console.ReadLine();

            if (personGroupId.Equals("return", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            TrainingStatus trainingStatus = null;

            try
            {
                trainingStatus = await faceClient.GetPersonGroupTrainingStatusAsync(personGroupId);
            }
            catch (Exception e)
            {
                return;
            }

            if (trainingStatus.Status == Status.Running)
            {
                Console.WriteLine("Task is still running...");
            }
            else
            {
                Console.WriteLine("Task complete!");
            }
        }

        private async static void TestImage()
        {
            string testImageFile = @"C:\Users\Richard.Rowe\Desktop\a.jpg";

            using (Stream s = File.OpenRead(testImageFile))
            {
                var faces = await faceClient.DetectAsync(s);
                var faceIds = faces.Select(face => face.FaceId).ToArray();

                var results = await faceClient.IdentifyAsync("myfriends", faceIds);
                foreach (var identifyResult in results)
                {
                    Console.WriteLine("Result of face: {0}", identifyResult.FaceId);
                    if (identifyResult.Candidates.Length == 0)
                    {
                        Console.WriteLine("No one identified");
                    }
                    else
                    {
                        // Get top 1 among all candidates returned
                        var candidateId = identifyResult.Candidates[0].PersonId;
                        var person = await faceClient.GetPersonAsync("myfriends", candidateId);
                        Console.WriteLine("Identified as {0}", person.Name);
                    }
                }
            }
        }

        private async static void MainMenu()
        {
            Console.WriteLine("1. Create person group");
            Console.WriteLine("2. Show available person groups");
            Console.WriteLine("3. Create person");
            Console.WriteLine("4. Show available people");
            Console.WriteLine("5. Upload photos for person and train program");
            Console.WriteLine("6. See if group is still training");
            Console.WriteLine("7. Test face recognition");
            Console.WriteLine("8. Exit");
            Console.Write("? ");
            var option = Console.ReadLine();

            switch (option)
            {
                case "1":
                    CreatePersonGroup();
                    break;
                case "2":
                    ShowAllPersonGroups().Wait();
                    break;
                case "3":
                    CreatePerson().Wait();
                    break;
                case "4":
                    ShowAllPeople().Wait();
                    break;
                case "5":
                    await UploadPhotosAndTrainProgram();
                    break;
                case "6":
                    CheckIfGroupIsStillTraining().Wait(1000000000);
                    break;
                case "7":
                    TestImage();
                    break;
                case "8":
                    NicelyExit("Thank you!", false);
                    break;
                default:
                    PrintError("Invalid menu option.");
                    break;
            }
        }

        private static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                NicelyExit("You must specify your subscription key as a command-line arg.");
            }

            string subKey = args[0];

            faceClient = new FaceServiceClient(subKey);
            Console.WriteLine("Client authorized successfully!");
            Console.WriteLine("");

            while (true)
            {
                MainMenu();
            }
        }
        //private static void Main(string[] args)
        //{
        //    // Create grabber. 
        //    FrameGrabber<Face[]> grabber = new FrameGrabber<Face[]>();

        //    // Create Face API Client.
        //    FaceServiceClient faceClient = new FaceServiceClient("<subscription key>");

        //    // Set up a listener for when we acquire a new frame.
        //    grabber.NewFrameProvided += (s, e) =>
        //    {
        //        Console.WriteLine("New frame acquired at {0}", e.Frame.Metadata.Timestamp);
        //    };

        //    // Set up Face API call.
        //    grabber.AnalysisFunction = async frame =>
        //    {
        //        Console.WriteLine("Submitting frame acquired at {0}", frame.Metadata.Timestamp);
        //        // Encode image and submit to Face API. 
        //        return await faceClient.DetectAsync(frame.Image.ToMemoryStream(".jpg"));
        //    };

        //    // Set up a listener for when we receive a new result from an API call. 
        //    grabber.NewResultAvailable += (s, e) =>
        //    {
        //        if (e.TimedOut)
        //            Console.WriteLine("API call timed out.");
        //        else if (e.Exception != null)
        //            Console.WriteLine("API call threw an exception.");
        //        else
        //            Console.WriteLine("New result received for frame acquired at {0}. {1} faces detected", e.Frame.Metadata.Timestamp, e.Analysis.Length);
        //    };

        //    // Tell grabber when to call API.
        //    // See also TriggerAnalysisOnPredicate
        //    grabber.TriggerAnalysisOnInterval(TimeSpan.FromMilliseconds(3000));

        //    // Start running in the background.
        //    grabber.StartProcessingCameraAsync().Wait();

        //    // Wait for keypress to stop
        //    Console.WriteLine("Press any key to stop...");
        //    Console.ReadKey();

        //    // Stop, blocking until done.
        //    grabber.StopProcessingAsync().Wait();
        //}
    }
}
