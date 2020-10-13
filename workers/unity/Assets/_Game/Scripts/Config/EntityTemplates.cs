using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.QueryBasedInterest;
using Spatialplayfab;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace BlankProject.Scripts.Config
{
    public static class EntityTemplates
    {
        public static EntityTemplate CreatePlayerEntityTemplate(EntityId entityId, string workerId, byte[] serializedArguments)
        {
            string playerId ="";
            string charId = ""; //use if players can have more than one character
            var serData = DeserializeArguments<string>(serializedArguments);
            if(serData != null)
            {
                var splitData = serData.Split('|');
                if(splitData.Length > 1)
                    playerId = splitData[1];
            }

            var clientAttribute = EntityTemplate.GetWorkerAccessAttribute(workerId);
            var serverAttribute = UnityGameLogicConnector.WorkerType;

            var position = new Vector3(0, 1f, 0);
            var coords = Coordinates.FromUnityVector(position);

            var template = new EntityTemplate();
            template.AddComponent(new Position.Snapshot(coords), clientAttribute);
            template.AddComponent(new Metadata.Snapshot("Player"), serverAttribute);
            template.AddComponent(new PlayerState.Snapshot(playerId, false), serverAttribute);

            PlayerLifecycleHelper.AddPlayerLifecycleComponents(template, workerId, serverAttribute);

            const int serverRadius = 500;
            var clientRadius = workerId.Contains(MobileClientWorkerConnector.WorkerType) ? 100 : 500;

            var serverQuery = InterestQuery.Query(Constraint.RelativeCylinder(serverRadius));
            var clientQuery = InterestQuery.Query(Constraint.RelativeCylinder(clientRadius));

            var interest = InterestTemplate.Create()
                .AddQueries<Metadata.Component>(serverQuery)
                .AddQueries<Position.Component>(clientQuery);
            template.AddComponent(interest.ToSnapshot(), serverAttribute);

            template.SetReadAccess(UnityClientConnector.WorkerType, MobileClientWorkerConnector.WorkerType, serverAttribute);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, serverAttribute);

            return template;
        }

        public static T DeserializeArguments<T>(byte[] serializedArguments)
        {
            using (var memoryStream = new MemoryStream())
            {
                var binaryFormatter = new BinaryFormatter();
                memoryStream.Write(serializedArguments, 0, serializedArguments.Length);
                memoryStream.Seek(0, SeekOrigin.Begin);
                return (T) binaryFormatter.Deserialize(memoryStream);
            }
        }
    }
}
