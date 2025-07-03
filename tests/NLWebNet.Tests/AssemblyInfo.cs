using Microsoft.VisualStudio.TestTools.UnitTesting;

// Enable parallel execution at assembly level for MSTest
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]
