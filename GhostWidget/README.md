# 유령농장 데스크톱 위젯

첨부한 원본 디자인을 그대로 살린 바탕화면 유령 친구입니다.

## 실행

PowerShell을 이 폴더에서 열고 아래 명령을 한 번 실행하세요.

```powershell
dotnet run
```

## 조작

- 유령을 **좌클릭**: 쓰다듬으며 친밀도가 올라갑니다.
- 유령을 **더블클릭**: 결투 연습을 합니다.
- 유령을 **드래그**: 원하는 화면 위치로 옮길 수 있습니다.
- 유령을 **우클릭**: 간식, 성장, 결투, 휴식, 유령 변경, 상태 확인, 종료 메뉴를 엽니다.
- **유령 농장 열기**: 20종 도감과 포획·육성·결투 콘텐츠를 엽니다.
- **포획하기**: 움직이는 유령 주위의 링이 몸에 겹치는 순간 봉인구를 던지세요.

유령은 작업표시줄을 제외한 화면 안에서 스스로 다음 목적지를 정해 부드럽게 이동합니다. 마우스를 가까이 대면 눈동자가 따라오고, 쓰다듬으면 하트가 떠오릅니다.

진행 상황은 자동 저장되며 앱을 다시 실행해도 포획한 유령, 루나, 봉인구, 레벨과 승수가 유지됩니다.
# Build delivery

Every completed change is published as a self-contained Windows single executable. The latest deliverable is always `GhostWidget/dist/GhostWidget.exe`.
