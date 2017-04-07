using System;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Collections.Specialized;
using Database;

namespace DatabasePopulator
{
    class Program
    {
        private class Person
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public Guid Guid { get; set; }
            public int ConstituentId { get; set; }
        }

        private static FaceServiceClient FaceClient { get; set; }
        private static string ApiKey { get; set; }
        private static string ConnectionString = @"Data Source=CHS6RICHARROW01\MSSQLSERVER14;Initial Catalog=OTG-CPA_04-2017;Integrated Security=SSPI";
        private static readonly string PathPrefix = "face_database";
        private static readonly string PersonGroupId = "otg";
        private static readonly string PersonGroupName = "Off the Grid";
        private static List<Person> People { get; set; }

        private static void InitialPrompt()
        {
            Console.WriteLine("You should only run this once on your machine. Change the connection string. Continue? (y/n)");
            if (Console.ReadLine().Equals("n"))
            {
                Environment.Exit(Environment.ExitCode);
            }
        }

        private static void InitializePeople()
        {
            People = new List<Person>
            {
                new Person
                {
                    Name = "alex",
                    Path = $"{AppDomain.CurrentDomain.BaseDirectory}\\..\\..\\{PathPrefix}\\alex\\",
                    ConstituentId = 915
                },
                new Person
                {
                    Name = "andrew",
                    Path = $"{AppDomain.CurrentDomain.BaseDirectory}\\..\\..\\{PathPrefix}\\andrew\\",
                    ConstituentId = 914
                },
                new Person
                {
                    Name = "doug",
                    Path = $"{AppDomain.CurrentDomain.BaseDirectory}\\..\\..\\{PathPrefix}\\doug\\",
                    ConstituentId = 917
                },
                new Person
                {
                    Name = "kevin",
                    Path = $"{AppDomain.CurrentDomain.BaseDirectory}\\..\\..\\{PathPrefix}\\kevin\\",
                    ConstituentId = 922
                },
                new Person
                {
                    Name = "marybeth",
                    Path = $"{AppDomain.CurrentDomain.BaseDirectory}\\..\\..\\{PathPrefix}\\marybeth\\",
                    ConstituentId = 930
                },
                new Person
                {
                    Name = "mike",
                    Path = $"{AppDomain.CurrentDomain.BaseDirectory}\\..\\..\\{PathPrefix}\\mike\\",
                    ConstituentId = 921
                },
                new Person
                {
                    Name = "ty",
                    Path = $"{AppDomain.CurrentDomain.BaseDirectory}\\..\\..\\{PathPrefix}\\ty\\",
                    ConstituentId = 916
                }
            };
        }

        public static void ReadCmdArgs(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("<apikey>");
                Console.ReadLine();
                Environment.Exit(Environment.ExitCode);
            }
            ApiKey = args[0];
        }

        public async static Task RunProgram()
        {
            FaceClient = new FaceServiceClient(ApiKey);
            Console.WriteLine("Creating person group...");
            FaceClient.CreatePersonGroupAsync(PersonGroupId, PersonGroupName).Wait();
            Console.WriteLine("Created!");

            Console.WriteLine("Creating people...");
            foreach (var p in People)
            {
                CreatePersonResult person = await FaceClient.CreatePersonAsync(PersonGroupId, p.Name);
                p.Guid = person.PersonId;
            }
            Console.WriteLine("Complete!");

            Console.WriteLine("Adding images...");
            int imageCount = 1;
            foreach (var p in People)
            {
                foreach (string imagePath in Directory.GetFiles(p.Path, "*.*"))
                {
                    using (Stream s = File.OpenRead(imagePath))
                    {
                        // Detect faces in the image and add to Anna
                        Console.WriteLine($"Adding image {imageCount}...");
                        FaceClient.AddPersonFaceAsync(PersonGroupId, p.Guid, s).Wait();
                        Task.Delay(5000).Wait();
                        imageCount++;
                    }
                }
            }
            Console.WriteLine("Complete!");

            Console.WriteLine("Training person group...");
            await FaceClient.TrainPersonGroupAsync(PersonGroupId);
            TrainingStatus trainingStatus = null;
            while (true)
            {
                trainingStatus = await FaceClient.GetPersonGroupTrainingStatusAsync(PersonGroupId);

                if (trainingStatus.Status != Status.Running)
                {
                    break;
                }

                await Task.Delay(5000);
            }
            Console.WriteLine("Complete!");
        }

        public async static Task WriteToDatabase()
        {
            string sql = "";
            foreach (var p in People)
            {
                sql += $"insert into dbo.Constituents (ConstituentId, PersonId) values ({p.ConstituentId}, '{p.Guid}');";
            }

            SqlHandler sqlHandler = new SqlHandler(ConnectionString);
            sqlHandler.ExecuteAsync(sql, null).Wait();
        }

        static void Main(string[] args)
        {
            InitialPrompt();
            InitializePeople();
            ReadCmdArgs(args);
            RunProgram().Wait();
            WriteToDatabase().Wait();
        }
    }
}
