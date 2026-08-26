using UnityEngine;

namespace ProjectLuna.CutscenePrototype.Authoring
{
    public sealed class LunaCutsceneBindingId : MonoBehaviour
    {
        [SerializeField] private string bindingId;
        public string BindingId => bindingId;

        public void Configure(string value)
        {
            bindingId = value;
        }
    }
}
