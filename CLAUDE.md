# CLAUDE.md

이 저장소에서 작업할 때 지켜야 할 규칙입니다.

## Git 워크플로우

- 작업이 끝나면 항상 `main` 브랜치에 직접 커밋·푸시합니다. 세션용 `claude/*` 브랜치에만 남겨두지 마세요.

## 배포용 실행 파일

- 구현 작업이 끝나면 항상 자체 포함(self-contained) 단일 실행 파일을 새로 빌드하고, 저장소 **최상위 폴더**의 `GhostWidget.exe`를 갱신해서 함께 커밋합니다. (`GhostWidget/dist/`는 로컬 빌드 산출물일 뿐이라 git에는 포함하지 않습니다.)
- 빌드 컨테이너에 `dotnet`이 없으면 먼저 설치합니다:
  ```bash
  curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  chmod +x /tmp/dotnet-install.sh
  /tmp/dotnet-install.sh --channel 10.0 --install-dir /root/.dotnet
  export PATH="/root/.dotnet:$PATH"
  export DOTNET_ROOT="/root/.dotnet"
  ```
- Linux 컨테이너에서도 빌드되도록 Windows 타겟팅을 강제하고, GitHub의 파일당 100MB 제한을 넘지 않도록 단일 파일 압축을 켜서 게시합니다:
  ```bash
  dotnet publish GhostWidget/GhostWidget.csproj -c Release -o GhostWidget/dist \
    -p:EnableWindowsTargeting=true -p:EnableCompressionInSingleFile=true
  cp GhostWidget/dist/GhostWidget.exe ./GhostWidget.exe
  ```
