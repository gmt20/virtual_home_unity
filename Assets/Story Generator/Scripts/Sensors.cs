using UnityEngine;
using System.Collections.Generic;
using StoryGenerator.Utilities;

namespace StoryGenerator
{
    public class MotionDetectionEvent
    {
        public int frameNumber;
        public string characterId;
        public string sensorId;
        public string roomName;
        public Vector3 characterPosition;

        public override string ToString()
        {
            return $"{frameNumber} {characterId} {sensorId} {roomName} {characterPosition.x} {characterPosition.y} {characterPosition.z}";
        }
    }

    public class Motion_sensor : MonoBehaviour
    {
        // Properties
        public float detectionRadius = 5.0f;
        public bool isActive = true;
        public string currentRoom { get; private set; }
        
        void Awake()
        {
            MotionSensorManager.RegisterSensor(this);
        }

        public void Initialize(string roomName, float radius)
        {
            currentRoom = roomName;
            detectionRadius = radius;
        }

        public bool IsPositionDetected(Vector3 position)
        {
            if (!isActive) return false;
            float distance = Vector3.Distance(transform.position, position);
            return distance <= detectionRadius;
        }

        public string SensorId => gameObject.name;

        void OnDestroy()
        {
            MotionSensorManager.UnregisterSensor(this);
        }
    }

    public static class MotionSensorManager 
    {
        private static List<Motion_sensor> allSensors = new List<Motion_sensor>();

        public static void RegisterSensor(Motion_sensor sensor)
        {
            if (!allSensors.Contains(sensor))
                allSensors.Add(sensor);
        }

        public static void UnregisterSensor(Motion_sensor sensor)
        {
            allSensors.Remove(sensor);
        }

        public static List<Motion_sensor> GetAllSensors() => allSensors;

        public static void Clear()
        {
            allSensors.Clear();
        }
    }

    public class MotionSensorPlacer
    {
        private Transform houseTransform;
        private List<GameObject> rooms;
        private string environmentId;

        public MotionSensorPlacer(Transform environmentTransform, string envId)
        {
            this.houseTransform = environmentTransform;
            this.environmentId = envId;
            this.rooms = StoryGenerator.Scripts.ScriptUtils.FindAllRooms(houseTransform);
        }

        public void PlaceSensorsInEnvironment()
        {
            if (rooms == null || rooms.Count == 0)
            {
                Debug.LogError($"No rooms found in environment {environmentId}");
                return;
            }

            foreach (GameObject room in rooms)
            {
                PlaceSensorsInRoom(room);
            }
        }

        private void PlaceSensorsInRoom(GameObject room)
        {
            Bounds roomBounds = GameObjectUtils.GetRoomBounds(room);
            float roomArea = roomBounds.size.x * roomBounds.size.z;
            int sensorCount = CalculateSensorCount(roomArea);
            List<Vector3> sensorPositions = CalculateSensorPositions(roomBounds, sensorCount);

            foreach (Vector3 position in sensorPositions)
            {
                CreateSensor(position, room);
            }
        }

        private int CalculateSensorCount(float roomArea)
        {
            const float SMALL_ROOM = 30f;  // Up to ~5x6m room = one corner sensor
            const float MEDIUM_ROOM = 60f; // Up to ~8x8m room = two opposite corners
            
            if (roomArea <= SMALL_ROOM) return 1;
            if (roomArea <= MEDIUM_ROOM) return 2;
            return 3; 
        }

        private List<Vector3> CalculateSensorPositions(Bounds roomBounds, int sensorCount)
        {
            List<Vector3> positions = new List<Vector3>();
            float sensorHeight = roomBounds.min.y + 2.4f; 
            float cornerOffset = 0.3f; 

            Vector3[] corners = new Vector3[4] 
            {
                new Vector3(roomBounds.min.x + cornerOffset, sensorHeight, roomBounds.max.z - cornerOffset), 
                new Vector3(roomBounds.max.x - cornerOffset, sensorHeight, roomBounds.max.z - cornerOffset), 
                new Vector3(roomBounds.max.x - cornerOffset, sensorHeight, roomBounds.min.z + cornerOffset), 
                new Vector3(roomBounds.min.x + cornerOffset, sensorHeight, roomBounds.min.z + cornerOffset)  
            };

            if (sensorCount == 1)
            {
                positions.Add(corners[0]); 
            }
            else if (sensorCount == 2)
            {
                positions.Add(corners[0]); 
                positions.Add(corners[2]); 
            }
            else
            {
                positions.Add(corners[0]); 
                positions.Add(corners[1]); 
                positions.Add(corners[3]); 
            }

            return positions;
        }

        private Motion_sensor CreateSensor(Vector3 position, GameObject room)
        {
            GameObject sensorObj = new GameObject($"MotionSensor_{room.name}");
            sensorObj.transform.parent = room.transform;
            sensorObj.transform.position = position;

            Motion_sensor sensor = sensorObj.AddComponent<Motion_sensor>();
            sensor.Initialize(room.name, radius: 5.0f);

            return sensor;
        }
    }
}