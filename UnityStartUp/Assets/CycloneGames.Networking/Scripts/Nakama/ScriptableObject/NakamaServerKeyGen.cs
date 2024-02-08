using UnityEngine;

namespace ZenGame.Nakama
{
    [CreateAssetMenu(menuName = "Nakama/NakamaServerKeyGen")]
    public class NakamaServerKeyGen : ScriptableObject
    {
        [SerializeField] private string srcFilePath;
        [SerializeField] private int offset;

        public string SrcFilePath
        {
            get => srcFilePath;
        }

        /// <summary>
        /// value 0 means no offset
        /// </summary>
        public string OffsetStr
        {
            get => offset == 0 ? string.Empty : offset.ToString();
        }
    }
}