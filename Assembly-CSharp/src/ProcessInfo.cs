using System.IO;

namespace AssemblyCSharp
{
    public class ProcessInfo
    {
        public const int currentVersion = 2;

        public int version;

        public int processId;

        public long elapsedTime;

        public long workingSet;

        public long privateMemorySize;

        public long pagedMemorySize;

        public long virtualMemorySize;

        public long nonpagedSystemMemorySize;

        public long pagedSystemMemorySize;

        public long totalProcessorTime;

        public long userProcessorTime;

        public long privilegedProcessorTime;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(version);
            writer.Write(processId);
            writer.Write(elapsedTime);
            writer.Write(workingSet);
            writer.Write(privateMemorySize);
            writer.Write(pagedMemorySize);
            writer.Write(virtualMemorySize);
            writer.Write(nonpagedSystemMemorySize);
            writer.Write(pagedSystemMemorySize);
            writer.Write(totalProcessorTime);
            writer.Write(userProcessorTime);
            writer.Write(privilegedProcessorTime);
        }

        public void Deserialize(BinaryReader reader)
        {
            version = reader.ReadInt32();
            processId = reader.ReadInt32();
            elapsedTime = reader.ReadInt64();
            workingSet = reader.ReadInt64();
            privateMemorySize = reader.ReadInt64();
            pagedMemorySize = reader.ReadInt64();
            virtualMemorySize = reader.ReadInt64();
            nonpagedSystemMemorySize = reader.ReadInt64();
            pagedSystemMemorySize = reader.ReadInt64();
            totalProcessorTime = reader.ReadInt64();
            userProcessorTime = reader.ReadInt64();
            privilegedProcessorTime = reader.ReadInt64();
        }

        public override string ToString()
        {
            return $"[ProcessInfo: version={version}, processId={processId}, elapsedTime={elapsedTime}, workingSet={workingSet}, privateMemorySize={privateMemorySize}, pagedMemorySize={pagedMemorySize}, virtualMemorySize={virtualMemorySize}, nonpagedSystemMemorySize={nonpagedSystemMemorySize}, pagedSystemMemorySize={pagedSystemMemorySize}, totalProcessorTime={totalProcessorTime}, userProcessorTime={userProcessorTime}, privilegedProcessorTime={privilegedProcessorTime}]";
        }
    }
}
