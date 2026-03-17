using System.Collections.Generic;
using UnityEditor.Compilation;

namespace Game.Editor.AI
{
    public class CompileErrorValidator : IModuleValidator
    {
        const string VALIDATOR_NAME = "CompileError";

        public void Validate(ValidationReport report)
        {
            // Runner가 컴파일 완료 후 수집한 메시지를 넘긴다. 직접 호출 시에는 빈 리스트로 아무것도 추가하지 않음.
        }

        public static void AddCompileMessagesToReport(ValidationReport report, List<CompilerMessage> messages)
        {
            if (messages == null)
                return;
            for (int i = 0; i < messages.Count; i++)
            {
                CompilerMessage msg = messages[i];
                if (msg.type == CompilerMessageType.Error)
                    report.AddError(VALIDATOR_NAME, msg.message, msg.file);
            }
        }
    }
}
