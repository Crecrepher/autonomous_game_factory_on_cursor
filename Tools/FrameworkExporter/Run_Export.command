#!/bin/bash
#
# FrameworkExporter - 더블클릭 실행기
# Finder에서 이 파일을 더블클릭하면 에셋 추출이 시작됩니다.
#

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

echo ""
echo "=============================================="
echo "  FrameworkExporter - 더블클릭 실행기"
echo "=============================================="
echo ""
echo "  작업 폴더: $SCRIPT_DIR"
echo ""

if ! command -v dotnet &> /dev/null; then
    echo "[ERROR] .NET SDK가 설치되어 있지 않습니다."
    echo "        https://dotnet.microsoft.com/download 에서 설치해주세요."
    echo ""
    echo "아무 키나 누르면 종료합니다..."
    read -n 1 -s
    exit 1
fi

echo "[INFO] .NET SDK 확인 완료: $(dotnet --version)"
echo ""

dotnet run -- --yes

EXIT_CODE=$?
echo ""

if [ $EXIT_CODE -eq 0 ]; then
    echo "=============================================="
    echo "  추출 완료! ExportPackage 폴더를 확인하세요."
    echo "=============================================="
else
    echo "=============================================="
    echo "  오류가 발생했습니다. (종료 코드: $EXIT_CODE)"
    echo "  위의 로그를 확인해주세요."
    echo "=============================================="
fi

echo ""
echo "아무 키나 누르면 종료합니다..."
read -n 1 -s
