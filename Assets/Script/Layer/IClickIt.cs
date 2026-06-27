public interface IClickIt
{
    // 카드를 클릭하여 들어 올렸을 때 호출할 함수
    void OnClickStart();

    // 카드를 드롭하여 마우스를 놓았을 때 호출할 함수
    void OnClickRelease();
}