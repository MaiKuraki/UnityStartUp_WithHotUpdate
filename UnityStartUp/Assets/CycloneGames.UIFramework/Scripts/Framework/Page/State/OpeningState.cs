namespace CycloneGames.UIFramework
{
    public class OpeningState : UIPageState
    {
        public override void OnEnter(UIPage page)
        {
            UnityEngine.Debug.Log($"[PageState] Opening: {page.PageName}");
        }

        public override void OnExit(UIPage page)
        {
            
        }
    }
}