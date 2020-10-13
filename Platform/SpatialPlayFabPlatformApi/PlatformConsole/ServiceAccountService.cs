using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Improbable.SpatialOS.ServiceAccount.V1Alpha1;
using PlayFab.ServerModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlatformConsole
{
    public class ServiceAccountService
    {
        /// <summary>
        ///     PLEASE REPLACE.
        ///     Your SpatialOS project name.
        ///     This scenario will create a new service account with read and write access to this project.
        /// </summary>
        private const string ProjectName = "PLEASE REPLACE.";

        /// <summary>
        ///     The threshold for when a service account's expiry time should be increased, in days relative to
        ///     the current time. If the service account expires in fewer days than this, its lifetime will be extended
        ///     by DaysToExpandServiceAccountBy days.
        /// </summary>
        private const int DaysRemainingAtWhichExpiryShouldBeIncreased = 20;

        /// <summary>
        ///     How many days to expand the service account's lifetime by if it is too close to expiry (as defined by
        ///     DaysRemainingAtWhichExpiryShouldBeIncreased), relative to the current time.
        /// </summary>
        private const int DaysToExpandServiceAccountBy = 29;

        private const int NumberOfServiceAccountsToCreate = 1;

        /// <summary>
        ///     PLEASE REPLACE.
        ///     The name given to service accounts created during setup.
        /// </summary>
        private const string ServiceAccountName = "sa_auther1";

        //private static PlatformRefreshTokenCredential CredentialWithProvidedToken;// = new PlatformRefreshTokenCredential(RefreshToken);

        private readonly static ServiceAccountServiceClient ServiceAccountServiceClient = ServiceAccountServiceClient.Create();// = ServiceAccountServiceClient.Create(credentials: CredentialWithProvidedToken);

        private static List<ServiceAccount> ServiceAccounts;

        /// <summary>
        ///     PLEASE REPLACE.
        ///     The SpatialOS Platform refresh token of a service account or a user account.
        /// </summary>
        private static string RefreshToken =>
            Environment.GetEnvironmentVariable("IMPROBABLE_REFRESH_TOKEN");


        /// <summary>
        ///     This contains the implementation of the "Service account maintenance" scenario.
        ///     1. Iterate over the service accounts in your project.
        ///     2. If a service account has expired, or is close to expiry, prolong the expiry time to some point in the
        ///     future.
        /// </summary>
        public async Task Main()
        {
            var tokenData = await PlayFab.PlayFabServerAPI.GetTitleInternalDataAsync(new GetTitleDataRequest()
            { Keys = new List<string>() { "SpatialosPlatformToken" } });

            if (tokenData.Error != null)
            {
                throw new Exception(tokenData.Error.GenerateErrorReport());
            }

            string token = null;

            tokenData.Result.Data.TryGetValue("SpatialosPlatformToken", out token);

            if (string.IsNullOrEmpty(token))
            {
                token = RefreshToken;
            }


            Console.WriteLine("Getting the service accounts that you have permission to view...");

            var serviceAccounts = ServiceAccountServiceClient.ListServiceAccounts(new ListServiceAccountsRequest
            {
                ProjectName = ProjectName
            });
            ServiceAccounts = serviceAccounts.ToList();

            // Set up some new service accounts
            await Setup();

            foreach (var serviceAccount in serviceAccounts)
            {
                // Calculate how many days it is until the service account expires, and output a message
                // depending on whether it has already expired, or is close to expiry

                var daysUntilExpiry =
                    Math.Floor((serviceAccount.ExpirationTime.ToDateTime() - DateTime.UtcNow).TotalDays);

                Console.WriteLine($"{serviceAccount.Id} {serviceAccount.Token}");

                Console.WriteLine(daysUntilExpiry < 0
                    ? $"Service account '{serviceAccount.Name}' expired {Math.Abs(daysUntilExpiry)} day(s) ago"
                    : $"Service account '{serviceAccount.Name}' will expire in {daysUntilExpiry} day(s)");

                // Now extend the lifetime by increasing the expiry time relative to the current time
                if ((serviceAccount.ExpirationTime.ToDateTime() - DateTime.UtcNow).TotalDays <=
                    DaysRemainingAtWhichExpiryShouldBeIncreased)
                {
                    Console.WriteLine(
                        $"Extending service account '{serviceAccount.Name}' expiry time by {DaysToExpandServiceAccountBy} days from now");

                    var updatedAccount = ServiceAccountServiceClient.UpdateServiceAccount(new UpdateServiceAccountRequest
                    {
                        Id = serviceAccount.Id,
                        ExpirationTime = DateTime.UtcNow.AddDays(DaysToExpandServiceAccountBy).ToTimestamp()
                    });
                }
            }

            Console.WriteLine("No more service accounts found");

            //Cleanup();
        }

        /// <summary>
        ///     This creates some service accounts with permissions to read and write to a project, which expire
        ///     in one week.
        /// </summary>
        private static async Task Setup()
        {
            var permProject = new Permission
            {
                Parts = { new RepeatedField<string> { "prj", ProjectName, "*" } },
                Verbs =
                {
                    new RepeatedField<Permission.Types.Verb>
                    {
                        Permission.Types.Verb.Read,
                        Permission.Types.Verb.Write
                    }
                }
            };

            var permServices = new Permission
            {
                Parts = { new RepeatedField<string> { "srv", "*" } },
                Verbs =
                {
                    new RepeatedField<Permission.Types.Verb>
                    {
                        Permission.Types.Verb.Read,
                        Permission.Types.Verb.Write,
                    }
                }
            };

            if (ServiceAccounts.Any(x => x.Name == ServiceAccountName))
                return;


            Console.WriteLine("Setting up for the scenario by creating new service accounts...");
            for (var i = 0; i < NumberOfServiceAccountsToCreate; i++)
            {
                var resp = ServiceAccountServiceClient.CreateServiceAccount(new CreateServiceAccountRequest
                {
                    Name = ServiceAccountName,
                    ProjectName = ProjectName,
                    Permissions = { new RepeatedField<Permission> { permProject, permServices } },
                    Lifetime = Duration.FromTimeSpan(new TimeSpan(30, 0, 0, 0)) // Let this service account live for one day
                });
                ServiceAccounts.Add(resp);

                await PlayFab.PlayFabServerAPI.SetTitleInternalDataAsync(new SetTitleDataRequest() { Key = "SpatialosPlatformToken", Value = resp.Token });

            }
        }

        /// <summary>
        ///     This deletes the service accounts created in the setup phase.
        /// </summary>
        private static void Cleanup()
        {
            Console.WriteLine("Cleaning up by deleting service accounts created during setup...");
            foreach (var sa in ServiceAccounts)
            {
                if (sa.Name == ServiceAccountName)
                    continue;

                ServiceAccountServiceClient.DeleteServiceAccount(new DeleteServiceAccountRequest
                {
                    Id = sa.Id
                });
            }
        }
    }
}
