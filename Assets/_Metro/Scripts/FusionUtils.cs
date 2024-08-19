using System.Collections;
using System.Collections.Generic;
using Fusion;
using NanoSockets;
using Oculus.Platform;
using UnityEngine;

public static class FusionUtils
{
    public static bool Contains<T>(NetworkArray<T> values, int length, T value)
    {
        for (int i = 0; i < length; i++)
        {
            if (values[i].Equals(value))
            {
                return true;
            }
        }
        return false;
    }

    public static int IndexOf<T>(NetworkArray<T> values, int length, T value)
    {
        for (int i = 0; i < length; i++)
        {
            if (values[i].Equals(value))
            {
                return i;
            }
        }
        return -1;
    }

#nullable enable
    public static T? Find<T> (NetworkArray<T> values, int length, System.Func<T, bool> predicate) where T : class
    {
        for (int i = 0; i < length; i++)
        {
            if (predicate(values[i]))
            {
                return values[i];
            }
        }
        return null;
    }
#nullable disable

    public static int Add<T>(NetworkArray<T> list, int length, T value)
    {
        if (length == list.Length)
        {
            throw new System.Exception("List is full");
        }
        list.Set(length, value);
        length++;
        return length;
    }

    public static int Insert<T>(NetworkArray<T> list, int length, int index, T value)
    {
        if (length == list.Length)
        {
            throw new System.Exception("List is full");
        }
        for (int i = length; i > index; i--)
        {
            list.Set(i, list[i - 1]);
        }
        list.Set(index, value);
        length++;
        return length;
    }

    public static int CopyFrom<T>(NetworkArray<T> dest, int destLength, IEnumerable<T> src, int srcLength)
    {
        if (srcLength > destLength)
        {
            // throw and print sizes
            throw new System.Exception("Destination list is too small: " + destLength + " < " + srcLength);
        }
        
        int i = 0;
        foreach (var value in src)
        {
            if (i >= srcLength)
            {
                break;
            }
            dest.Set(i, value);
            i++;
        }
        return i;
    }

    public static int RemoveAt<T>(NetworkArray<T> list, int length, int index)
    {
        for (int i = index; i < length - 1; i++)
        {
            list.Set(i, list[i + 1]);
        }
        length--;
        return length;
    }

    public static int Remove<T>(NetworkArray<T> list, int length, T value)
    {
        for (int i = 0; i < length; i++)
        {
            if (list[i].Equals(value))
            {
                return RemoveAt(list, length, i);
            }
        }
        return length;
    }

    public static IEnumerable<T> GetFilledElements<T>(this NetworkArray<T> networkArray, int arrayCount)
    {
        for (int i = 0; i < arrayCount; i++)
        {
            yield return networkArray[i];
        }
    }

    public static GameObject createNetworkObject()
    {
        // load the prefab from path Assets/_Metro/Resources/Prefabs/NetworkObjectEmpty.prefab
        // instantiate the prefab
        GameObject prefab = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/NetworkObjectEmpty"));
        return prefab;
    }

    public static NetworkObject initializeNetworkObject(NetworkRunner runner, string name, GameObject template = null)
    {
        if (runner.IsClient){
            GameObject go = GameObject.Find(name);
            if (go != null)
            {
                if (go.GetComponent<NetworkObject>() != null)
                {
                    return go.GetComponent<NetworkObject>();
                }
            }
            return null;
        }

        if (template == null)
        {
            template = createNetworkObject();
        }
        // // inject the go into BakingObjectProvider's customPrefabCreators
        // uint flag = BakingObjectProvider.AddCustomPrefabCreator((runner, context) =>
        // {
        //     return template;
        // });
        template.AddComponent<NetworkObject>();
        var nt = template.AddComponent<NetworkTransform>();
        nt.SyncParent = true;
        template.AddComponent<NetworkName>();
        BakingObjectProvider.Baker.Bake(template);
        runner.MoveToRunnerScene(template);
        NetworkObject instance = runner.Spawn(template, onBeforeSpawned: (runner, obj) =>
        {
            obj.GetComponent<NetworkName>().syncedName = name;
        });
        return instance;
    }
}

public class BakingObjectProvider : NetworkObjectProviderDefault
{
    // For this sample, we are using very high flag values to indicate custom.
    // Other values will fall through the default instantiation handling.
    public const uint CUSTOM_PREFAB_FLAG_MIN = 100000;
    public const uint CUSTOM_PREFAB_FLAG_MAX = 200000;
    private static uint _nextCustomPrefabFlag = CUSTOM_PREFAB_FLAG_MIN;

    // The NetworkObjectBaker class can be reused and is Runner independent.
    private static NetworkObjectBaker _baker;
    public static NetworkObjectBaker Baker => _baker ??= new NetworkObjectBaker();

    // static to keep track with map of PrefabId to creation function to return a go
    // parameter NetworkRunner, context, return GameObject
    private static Dictionary<uint, System.Func<NetworkRunner, NetworkPrefabAcquireContext, GameObject>> customPrefabCreators = new Dictionary<uint, System.Func<NetworkRunner, NetworkPrefabAcquireContext, GameObject>>();
    // static function to add a custom prefab creator
    public static uint AddCustomPrefabCreator(System.Func<NetworkRunner, NetworkPrefabAcquireContext, GameObject> creator)
    {
        uint flag = _nextCustomPrefabFlag;
        customPrefabCreators[flag] = creator;
        _nextCustomPrefabFlag += 1;
        return flag;
    }

    public override NetworkObjectAcquireResult AcquirePrefabInstance(NetworkRunner runner, in NetworkPrefabAcquireContext context, out NetworkObject result)
    {
        // Check if the PrefabId is in the customPrefabCreators map
        if (customPrefabCreators.TryGetValue(context.PrefabId.RawValue, out var creator))
        {
            // Call the creator function to get the GameObject
            var go = creator(runner, context);
            var no = go.GetComponent<NetworkObject>();

            // Baking is required for the NetworkObject to be valid for spawning.
            Baker.Bake(go);

            // Move the object to the applicable Runner Scene/PhysicsScene/DontDestroyOnLoad
            // These implementations exist in the INetworkSceneManager assigned to the runner.
            if (context.DontDestroyOnLoad)
            {
                runner.MakeDontDestroyOnLoad(go);
            }
            else
            {
                runner.MoveToRunnerScene(go);
            }

            // We are finished. Return the NetworkObject and report success.
            result = no;
            return NetworkObjectAcquireResult.Success;
        }

        // For all other spawns, use the default spawning.
        return base.AcquirePrefabInstance(runner, context, out result);
    }
}
