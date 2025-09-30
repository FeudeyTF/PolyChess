using System.Reflection;

namespace PolyChess.Core.Commands.Parametrized
{
    internal interface IParametrizedCommandBuilder
    {
        public string GetCommandName(MethodInfo methodInfo);

        public List<ICommandParameter> GetCommandParameters(MethodInfo methodInfo);

        public bool IsParameterOptional(MethodInfo methodInfo);
    }
}
