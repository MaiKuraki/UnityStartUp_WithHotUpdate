namespace CycloneGames.UIFramework
{
    public class ClosingState : UIPageState
    {
        public override void OnEnter(UIPage page)
        {
            UnityEngine.Debug.Log($"[PageState] Closing: {page.PageName}");
        }

        public override void OnExit(UIPage page)
        {
            
        }
    }
}