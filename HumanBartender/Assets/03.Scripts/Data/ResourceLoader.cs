using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class ResourceLoader
{
    /// <summary>
    /// 로드 성공 시 handle 반환, 실패/예외 시 null 반환.
    /// 실패한 handle은 내부에서 즉시 릴리즈.
    /// TODO : ExistsInAddressables 함수는 순수 파일 존재 여부만 검사하기에 따로 또 처리해야함.
    /// </summary>
    public async static UniTask<AsyncOperationHandle<T>?> TryLoadAsync<T>(
        string address,
        CancellationToken token)
    {
        //해당 주소 존재 여부 확인
        bool exists = await ExistsInAddressables(address, token);
        if (!exists)
        {
            Debug.LogWarning($"Addressable key not found: {address}");
            return null;
        }

        var handle = Addressables.LoadAssetAsync<T>(address);
        
        try
        {
            await handle.ToUniTask(cancellationToken:token);

            if (handle.Status == AsyncOperationStatus.Succeeded)
                return handle;

            // 로드 실패 — 핸들 즉시 릴리즈
            if (handle.IsValid()) Addressables.Release(handle);
            return null;
        }
        catch (OperationCanceledException)
        {
            if (handle.IsValid()) Addressables.Release(handle);
            throw;
        }
        catch (Exception e)
        {
            Logger.Log(e.Message);
            if (handle.IsValid()) Addressables.Release(handle);
            return null;
        }
    }

    public async static UniTask<bool> ExistsInAddressables(string key, CancellationToken token)
    {
        var handle = Addressables.LoadResourceLocationsAsync(key);

        try
        {
            var locations = await handle.ToUniTask(cancellationToken:token);
            bool exists = locations != null && locations.Count > 0;
            return exists;
        }
        finally
        {
            if(handle.IsValid()) Addressables.Release(handle);
        }
    }

    public static void ReleaseHandle<T>(ref AsyncOperationHandle<T>? handle)
    {
        if (handle.HasValue && handle.Value.IsValid())
        {
            Addressables.Release(handle.Value);
            handle = null;
        }
    }
}
