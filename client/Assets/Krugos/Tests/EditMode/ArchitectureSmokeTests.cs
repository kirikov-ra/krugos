using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Compilation;
using Assembly = System.Reflection.Assembly;

namespace Krugos.Tests.EditMode
{
    public sealed class ArchitectureSmokeTests
    {
        [TestCase("Krugos.Domain")]
        [TestCase("Krugos.Application")]
        public void CoreAssemblyHasNoUnityDependencies(string assemblyName)
        {
            var assembly = Assembly.Load(assemblyName);

            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                Assert.That(reference.Name.StartsWith("Unity", StringComparison.Ordinal),
                    Is.False, assemblyName + " must remain ordinary C#: " + reference.Name);
                if (reference.Name.StartsWith("Krugos.", StringComparison.Ordinal))
                {
                    Assert.That(assemblyName == "Krugos.Application"
                        && reference.Name == "Krugos.Domain", Is.True,
                        assemblyName + " has a forbidden layer reference: " + reference.Name);
                }
            }
        }

        [Test]
        public void CompilerReferencesRespectLayerBoundaries()
        {
            var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);
            var names = new[] { "Domain", "Application", "Infrastructure", "Presentation", "Editor" };
            var allowed = new[]
            {
                Array.Empty<string>(),
                new[] { "Krugos.Domain" },
                new[] { "Krugos.Application", "Krugos.Domain" },
                new[] { "Krugos.Application", "Krugos.Domain" },
                Array.Empty<string>()
            };

            for (var i = 0; i < names.Length; i++)
            {
                var name = "Krugos." + names[i];
                var assembly = assemblies.Single(item => item.name == name);
                var layerReferences = assembly.assemblyReferences.Select(item => item.name)
                    .Where(item => item.StartsWith("Krugos.", StringComparison.Ordinal));
                Assert.That(layerReferences, Is.EquivalentTo(allowed[i]), name);

                if (name == "Krugos.Domain" || name == "Krugos.Application")
                {
                    var unityReferences = assembly.allReferences.Select(Path.GetFileName)
                        .Where(item => item.StartsWith("Unity", StringComparison.Ordinal));
                    Assert.That(unityReferences, Is.Empty, name);
                }
            }
        }

        [Test]
        public void PlayerCompilationExcludesEditorAndTests()
        {
            var names = CompilationPipeline.GetAssemblies(AssembliesType.Player)
                .Select(assembly => assembly.name).ToArray();

            Assert.That(names, Does.Contain("Krugos.Domain"));
            Assert.That(names, Does.Contain("Krugos.Application"));
            Assert.That(names, Does.Contain("Krugos.Infrastructure"));
            Assert.That(names, Does.Contain("Krugos.Presentation"));
            Assert.That(names, Does.Not.Contain("Krugos.Editor"));
            Assert.That(names, Does.Not.Contain("Krugos.Tests.EditMode"));
        }
    }
}
