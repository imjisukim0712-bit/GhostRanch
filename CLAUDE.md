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
  ```

### 버전 관리

- `GhostWidget/GhostWidget.csproj`의 `<Version>`을 새 빌드마다 patch 버전을 1씩 올립니다 (예: 1.0.0 → 1.0.1).
- 새로 빌드한 실행 파일은 두 곳에 둡니다:
  - `releases/GhostWidget-v<버전>.exe` — 해당 버전의 아카이브본. 기존 파일은 절대 덮어쓰거나 지우지 않고 계속 누적합니다.
  - `./GhostWidget.exe` (최상위 폴더) — 최신 버전으로 항상 덮어씁니다. 버전 번호 없이 "현재 최신"만 가리킵니다.
  ```bash
  cp GhostWidget/dist/GhostWidget.exe releases/GhostWidget-v<버전>.exe
  cp GhostWidget/dist/GhostWidget.exe ./GhostWidget.exe
  ```
- `releases/`는 바이너리가 계속 쌓이는 폴더라 저장소 용량이 매 빌드마다 커집니다. 용량이 부담되면 Git LFS 전환이나 오래된 버전 정리를 사용자와 상의하세요.
