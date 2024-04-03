using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Jobs;

namespace Chummer.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net80, baseline: true)]
    [MemoryDiagnoser]
    [EtwProfiler]
    public class LoadingCharacterBenchmark
    {
        public static IEnumerable<FileInfo> Characters { get; }
        static LoadingCharacterBenchmark()
        {
            DirectoryInfo dir = new(AppDomain.CurrentDomain.BaseDirectory);
            Characters = dir.GetDirectories()
                .Single(d => d.Name == "TestFiles")
                .EnumerateFiles("*.chum5")
                .Take(1)
                .ToArray();
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            Utils.IsUnitTest = true;
            Utils.IsUnitTestForUI = false;
        }


        [Benchmark]
        [ArgumentsSource(nameof(Characters))]
        public Character LoadSingleCharacter(FileInfo Character)
        {
            Character character = new Character();
            character.FileName = Character.FullName;
            if (!character.Load())
            {
                throw new InvalidOperationException($"Character failed to load: {Character}");
            }
            return character;
        }

        [Benchmark]
        public List<Character> LoadAllCharacters()
        {
            List<Character> characters = new List<Character>();
            foreach (FileInfo file in Characters)
            {
                Character character = new Character();
                character.FileName = file.FullName;
                if (!character.Load())
                {
                    throw new InvalidOperationException($"Character failed to load: {file}");
                }
                characters.Add(character);
            }
            return characters;
        }
    }
}
