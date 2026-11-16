using System;

public abstract class IAdPlatform
{
    /* 调起侧边栏 */
    public abstract void NavigateToScene(Action onSuccess, Action<string> onFail);

    /* 激励视频广告 */
    public abstract void ShowRewardedVideoAd(string adId, Action onSuccess, Action<string> onFail);

    /* 插屏广告
     * 返回值：是否真的把广告展示出来了。
     *   返回 false 的情况：广告模块启动 15 秒内、两次插屏间隔不足 30 秒、实例未加载完成、平台异常。
     *     调用方必须自己兜底，否则玩家会被卡住。
     * onClosed：广告被关闭（展示结束）时回调。
     * onFail：广告没能展示时回调，reason 是原因。
     * 注意：插屏不保证看完，没有「看完才发奖励」的概念。 */
    public abstract bool ShowInterstitialAd(string adId, Action onClosed, Action<string> onFail);

    /* 兼容旧写法：不关心回调的调用方（比如定时插屏）直接用这个 */
    public void ShowInterstitialAd(string adId)
    {
        this.ShowInterstitialAd(adId, null, null);
    }

    /** 游戏分享 */
    public abstract void ShareGame(Action<bool> callBack);
}
