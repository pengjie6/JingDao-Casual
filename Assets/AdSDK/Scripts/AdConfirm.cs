using System;
using _Content.InGame.Managers;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 激励广告弹窗
/// </summary>
public class AdConfirm : MonoBehaviour
{
    private static AdConfirm instance;

    [SerializeField] private Button btnClose;
    [SerializeField] private Button btnConfirm;
    private Action onCompleted;

    public static void Show(Action callBack)
    {
        if (instance == null)
        {
            // 加载 AdConfirm 预制体
            GameObject prefab = Resources.Load<GameObject>("AdConfirm");
            if (prefab == null)
            {
                Debug.LogError("AdConfirm 预制体未找到，请确保其位于 Resources 文件夹中。");
                return;
            }

            // 实例化 AdConfirm
            GameObject adConfirmInstance = Instantiate(prefab);
            instance = adConfirmInstance.GetComponent<AdConfirm>();

            if (instance == null)
            {
                Debug.LogError("AdConfirm 脚本未附加到预制体上，请检查预制体设置。");
                Destroy(adConfirmInstance);
                return;
            }
        }

        // 设置回调并显示弹窗
        if (callBack != null)
            instance.onCompleted = callBack;
        instance.gameObject.SetActive(true);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        //gameObject.SetActive(false);

        // jiliBtn 是用 SetActive(true) 直接打开本弹窗的，不走 Show()，所以不会有回调。
        // 这条路径的用途就是「看激励广告 +1 次复活机会」，这里给个默认回调。
        if (onCompleted == null)
            onCompleted = GrantReviveChance;

        btnClose?.onClick.AddListener(this.OnClose);
        btnConfirm?.onClick.AddListener(this.OnConfirm);
    }

    /// <summary>默认用途：看完广告给玩家加一次复活机会。</summary>
    private static void GrantReviveChance()
    {
        if (GameplayManager.Instance == null)
        {
            Debug.LogError("[广告] 场景里没有 GameplayManager，无法增加复活次数");
            return;
        }

        GameplayManager.Instance.AddReviveChance();
    }


    private void OnClose()
    {
        // 关闭后恢复默认用途，避免把上一次的回调留给下一次打开
        onCompleted = GrantReviveChance;
        this.gameObject.SetActive(false);
    }

    void OnConfirm()
    {
#if UNITY_EDITOR
        onCompleted?.Invoke();
        this.OnClose();
#else
        if (AdManager.Instance == null)
        {
            Debug.LogError("[广告] 场景里没有 AdManager，无法播放广告");
            OnAdError("广告服务未初始化");
            return;
        }
        AdManager.Instance.ShowRewardedVideoAd(OnAdWatched, OnAdError);
#endif
    }

    private void OnAdWatched()
    {
        Debug.Log("广告观看完成");
        onCompleted?.Invoke();
        OnClose();
    }

    private void OnAdError(string errorMessage)
    {
        Debug.LogError($"广告播放失败: {errorMessage}");
        // 加载 AdError 预制体
        GameObject adErrorPrefab = Resources.Load<GameObject>("AdError");
        if (adErrorPrefab != null)
        {
            // 实例化 AdError
            GameObject adErrorInstance = Instantiate(adErrorPrefab, transform.position, Quaternion.identity);
            // 1 秒后销毁实例
            Destroy(adErrorInstance, 1.5f);
        }
        else
        {
            Debug.LogError("AdError 预制体未找到，请确保其位于 Resources 文件夹中。");
        }
    }
}
