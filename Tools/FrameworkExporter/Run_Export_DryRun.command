#!/bin/bash
#
# FrameworkExporter - 미리보기 (Dry Run)
# 실제 파일 복사 없이 어떤 파일이 추출될지 미리 확인합니다.
#

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

echo ""
echo "=============================================="
echo "  FrameworkExporter - 미리보기 (Dry Run)"
echo "  ※ 실제 파일은 복사되지 않습니다"
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

dotnet run -- --dry-run --yes

echo ""
echo "=============================================="
echo "  미리보기 완료!"
echo "  실제 추출하려면 Run_Export.command를 실행하세요."
echo "=============================================="
echo ""
echo "아무 키나 누르면 종료합니다..."
read -n 1 -s
