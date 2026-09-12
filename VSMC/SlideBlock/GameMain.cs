// -------------------------------------------------------------------------------------------------------------------------------------------------------------
// Author: 3dapi (https://github.com/3dapi)
// -------------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using Vortice.Mathematics;

class GameMain : G2AppBase
{
    public override System.Drawing.Size ScreenSize => GameGlobal.ScreenSize;
    public override string GameName => GameGlobal.GameName;

    // 1. 이미지 텍스쳐 변수 선언
    private G2Texture? _bgTexture = null;

    protected override void Initialize()
    {
        //---------------------------------------
        // 게임 관련 객체를 생성합니다.
        //---------------------------------------
        // 이미지 파일 로드 (실제 파일 경로에 맞게 조정)
        _bgTexture = new G2Texture("resource/grid_map/map.png");
    }

    protected override void Update()
    {
        double elapsed = TotalTime;

        this.ClearColor = new Color4(
            red: (float)(Math.Sin(elapsed) * 0.5 + 0.5),
            green: (float)(Math.Sin(elapsed + Math.PI / 2.0) * 0.5 + 0.5),
            blue: (float)(Math.Sin(elapsed + Math.PI) * 0.5 + 0.5),
            alpha: 1.0f);

        //---------------------------------------
        // 게임 관련 객체를 갱신합니다.
        //---------------------------------------
    }

    protected override void Render()
    {
        //---------------------------------------
        // 게임 관련 객체를 렌더링 합니다.
        //---------------------------------------
        // 화면의 (10, 10) 좌표에 이미지 출력
        _bgTexture?.Draw(200, 50);
    }

    public override void Dispose()
    {
        //---------------------------------------
        // 게임 관련 객체를 해제합니다.
        //---------------------------------------
        _bgTexture?.Dispose();
        _bgTexture = null;

        base.Dispose();
    }
}