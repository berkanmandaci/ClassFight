using System;
using System.Collections.Generic;
using _Project.Runtime.Core.Extensions.Singleton;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace ProjectV3.Client._ProjectV3.Runtime.Client.Scripts.Core
{
    public class BundleModel : SingletonBehaviour<BundleModel>
    {

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
        }
        public async UniTask<GameObject> InstantiatePrefab(string key, Transform parent = null)
        {
            var handle = Addressables.InstantiateAsync(key, parent);

            try
            {
                await handle.Task;
            }
            catch (Exception e)
            {
                LogModel.Instance.Error(e);
                throw;
            }

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return handle.Result;
            }

            LogModel.Instance.Error(handle.OperationException);

            throw handle.OperationException;
        }

        public async UniTask<List<T>> InstantiatePrefabList<T>(string key, int count, Transform parent = null)
        {
            var taskList = new List<UniTask>();
            var list = new List<T>();

            for (int i = 0; i < count; i++)
            {
                taskList.Add(Task(list));
            }
            await UniTask.WhenAll(taskList);

            return list;


            async UniTask Task(ICollection<T> listT)
            {
                var go = await InstantiatePrefab(key, parent);
                var component = go.GetComponent<T>();
                listT.Add(component);
            }
        }



        public async UniTask<T> LoadAsset<T>(string key)
        {
            var handle = Addressables.LoadAssetAsync<T>(key);

            try
            {
                await handle.Task;
            }
            catch (Exception e)
            {
                LogModel.Instance.Error(e);
                throw;
            }

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return handle.Result;
            }

            LogModel.Instance.Warning(handle.OperationException.ToString());

            throw handle.OperationException;
        }


        public async UniTask<SceneInstance> LoadScene(string key, LoadSceneMode sceneMode = LoadSceneMode.Additive)
        {
            var handle = Addressables.LoadSceneAsync(key, sceneMode);

            try
            {
                await handle.Task;
            }
            catch (Exception e)
            {
                LogModel.Instance.Error(e);
                throw;
            }

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return handle.Result;
            }

            LogModel.Instance.Warning(handle.OperationException.ToString());

            throw handle.OperationException;
        }
    }
}
