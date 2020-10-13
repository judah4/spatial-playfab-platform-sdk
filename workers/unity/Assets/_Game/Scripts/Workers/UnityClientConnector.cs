using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Representation;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using UnityEngine;

namespace BlankProject
{
    public class UnityClientConnector : WorkerConnector
    {
        [SerializeField] private EntityRepresentationMapping entityRepresentationMapping = default;

        public const string WorkerType = "UnityClient";

        private async void Start()
        {
            var connParams = CreateConnectionParameters(WorkerType);

            var builder = new SpatialOSConnectionHandlerBuilder()
                .SetConnectionParameters(connParams);

            if (!Application.isEditor)
            {
                var initializer = new DragonConnectionFlowInitializer(true);
                var commandLineInitializer = new CommandLineConnectionFlowInitializer();
                switch (initializer.GetConnectionService())
                {
                    case ConnectionService.Receptionist:
                        builder.SetConnectionFlow(new ReceptionistFlow(CreateNewWorkerId(WorkerType), commandLineInitializer));
                        break;
                    case ConnectionService.Locator:
                        builder.SetConnectionFlow(new LocatorFlow(initializer));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                builder.SetConnectionFlow(new ReceptionistFlow(CreateNewWorkerId(WorkerType)));
            }

            await Connect(builder, new ForwardingDispatcher()).ConfigureAwait(false);
        }

        protected override void HandleWorkerConnectionEstablished()
        {
            PlayerLifecycleHelper.AddClientSystems(Worker.World, false);
            GameObjectCreationHelper.EnableStandardGameObjectCreation(Worker.World, entityRepresentationMapping);

            var creds = "";

            if (LoginDetails.Instance.PlayfabSessionTicket != null)
                creds = $"{LoginDetails.Instance.characterIdRequested}|{LoginDetails.Instance.PlayerId}";

            var data = SerializeArguments(creds);
            Worker.World.GetOrCreateSystem<SendCreatePlayerRequestSystem>().RequestPlayerCreation(data);
        }

        public static byte[] SerializeArguments(object playerCreationArguments)
        {
            using (var memoryStream = new MemoryStream())
            {
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, playerCreationArguments);
                return memoryStream.ToArray();
            }
        }

    }


    public class DragonConnectionFlowInitializer : IConnectionFlowInitializer<LocatorFlow>
    {
        private bool _connectToCloud = false;

        public DragonConnectionFlowInitializer(bool connectToCloud)
        {
            _connectToCloud = connectToCloud;
        }


        public ConnectionService GetConnectionService()
        {
            if (LoginDetails.Instance.DeploymentSelected != null &&
                string.IsNullOrEmpty(LoginDetails.Instance.DeploymentSelected.DeploymentName) == false)
            {
                if (LoginDetails.Instance.DeploymentSelected.DeploymentName == "Local")
                {
                    return ConnectionService.Receptionist;

                }

                return ConnectionService.Locator;

            }

            // This is the expected behavior for any server-worker.
            return ConnectionService.Receptionist;

        }

        public void Initialize(LocatorFlow alphaLocator)
        {
            var playerToken = "";
            var loginToken = "";

            if (!string.IsNullOrEmpty(LoginDetails.Instance.PlayerIdentityToken))
            {
                playerToken = LoginDetails.Instance.PlayerIdentityToken;
                loginToken = LoginDetails.Instance.DeploymentSelected.LoginToken;
                alphaLocator.UseDevAuthFlow = false;
            }

            alphaLocator.LocatorHost = RuntimeConfigDefaults.LocatorHost;
            alphaLocator.PlayerIdentityToken = playerToken;
            alphaLocator.LoginToken = loginToken;
            alphaLocator.UseInsecureConnection = false;

        }
    }
}
