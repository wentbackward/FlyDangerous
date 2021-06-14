using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

namespace Engine {
    public class LevelDataVector3<T> {
        public T x;
        public T y;
        public T z;
    }

    public enum Location {
        NullSpace,
        TestSpaceStation,
        TerrainV1,
        TerrainV2,
        BasicCircuit
    }

    public enum Environment {
        PlanetOrbitBottom,
        PlanetOrbitTop,
        SunriseClear,
        NoonClear,
        NoonCloudy,
        NoonStormy,
        SunsetClear,
        SunsetCloudy,
        NightClear,
        NightCloudy,
        Circuits
    }
    
    public enum RaceType {
        None,
        Sprint,
        Laps,
        Editor
    }

    public class CheckpointLocation {
        public CheckpointType type;
        public LevelDataVector3<float> position;
        public LevelDataVector3<float> rotation;
    }
    
    public class LevelData {
        public int version = 1;     // if the version is not this then we'll use the legacy terrain
        public string name = "";
        public Location location = Location.NullSpace;
        public Environment environment = Environment.NoonClear;
        public string terrainSeed = "";
        public LevelDataVector3<float> startPosition = new LevelDataVector3<float>();
        public LevelDataVector3<float> startRotation = new LevelDataVector3<float>();
        public RaceType raceType = RaceType.None;
        [CanBeNull] public List<CheckpointLocation> checkpoints;
        
        public string ToJsonString() {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [CanBeNull]
        public static LevelData FromJsonString(string json) {
            try {
                return JsonConvert.DeserializeObject<LevelData>(json);
            }
            catch (Exception e) {
                #if UNITY_EDITOR
                    Debug.LogWarning(e.Message);
                #endif
                return null;
            }
        }
    }
}