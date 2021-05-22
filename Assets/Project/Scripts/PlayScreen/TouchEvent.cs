using UnityEngine;

public class TouchEvent : MonoBehaviour
{
    public PlayScreenProcessManager playScreenProcessManager;

    public void ONClick0() => playScreenProcessManager.lane0[0].ONLaneTapped(0);

    public void ONClick1() => playScreenProcessManager.lane1[0].ONLaneTapped(1);

    public void ONClick2() => playScreenProcessManager.lane2[0].ONLaneTapped(2);

    public void ONClick3() => playScreenProcessManager.lane3[0].ONLaneTapped(3);

    public void ONClick4() => playScreenProcessManager.lane4[0].ONLaneTapped(4);
}