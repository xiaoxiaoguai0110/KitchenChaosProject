using UnityEngine;

/// <summary>
/// 为兼容菜单场景中的旧组件而暂时保留。
/// 静态事件现在由各自的拥有者在 SubsystemRegistration 阶段重置，不再依赖场景 Start 顺序。
/// </summary>
public sealed class ClearStaticData : MonoBehaviour
{
}
