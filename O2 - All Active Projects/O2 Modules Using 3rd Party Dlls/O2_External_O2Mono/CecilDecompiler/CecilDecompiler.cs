// This file is part of the OWASP O2 Platform (http://www.owasp.org/index.php/OWASP_O2_Platform) and is released under the Apache 2.0 License (http://www.apache.org/licenses/LICENSE-2.0)
using System;
using System.IO;
using System.Text;
using Cecil.Decompiler.Cil;
using Cecil.Decompiler.ControlFlow;
using Cecil.Decompiler.Languages;
using Mono.Cecil;
using Mono.Cecil.Cil;
using O2.DotNetWrappers.Windows;

namespace O2.External.O2Mono.CecilDecompiler
{
    public class CecilDecompiler
    {
    

        public string getSourceCode(string testExe)
        {
            var assembly = AssemblyFactory.GetAssembly(testExe);
            if (assembly != null)
            {
                var sourceCode = new StringBuilder();
                foreach(var methods in O2.External.O2Mono.MonoCecil.CecilUtils.getMethods(assembly))
                {
                    var methodCode = getSourceCode(methods);
                    sourceCode.Append(methodCode);                    
                }
                return sourceCode.ToString();
            }
            return "";
            
        }

        public string getSourceCode(MethodDefinition method)
        {
            try
            {
                //var controlFlowGraph = ControlFlowGraph.Create(method);

                //var body = method.Body.Decompile(language);

                ILanguage language = CSharp.GetLanguage(CSharpVersion.V1);
                var stringWriter = new StringWriter();
                ILanguageWriter writer = language.GetWriter(new PlainTextFormatter(stringWriter));
                writer.Write(method);


                var memoryStream = new MemoryStream();
                var streamWriter = new StreamWriter(memoryStream);

                ILanguageWriter writer2 = language.GetWriter(new PlainTextFormatter(streamWriter));

                writer2.Write(method);

                memoryStream.Flush();
                return stringWriter.ToString();
            }
            catch (Exception ex)
            {
                DI.log.error("in getSourceCode: {0}", ex.Message);
                return "Error in creating source code from IL: " + ex.Message;
                
            }            
        }


        public string getIL(MethodDefinition method)
        {
            return getIL_usingControlFLowGraph(method);
            //return getIL_usingRawIlParsing(method);
        }

        public string getIL_usingRawIlParsing(MethodDefinition methodDefinition)
        {
            try
            {
                /*var stringWriter = new StringWriter();
                foreach (Instruction instruction in methodDefinition.Body.Instructions)
                {                    
                    Formatter.WriteInstruction(stringWriter, instruction);
                    stringWriter.WriteLine();
                }
                return stringWriter.ToString();
                 * */
                var ilCode = new StringBuilder();
                for (int i = 0; i < methodDefinition.Body.Instructions.Count; i++)
                {
                    Instruction instruction = methodDefinition.Body.Instructions[i];
                    string instructionText = instruction.OpCode.ToString();
                    if (instruction.Operand != null)
                        instructionText += "   ...   " + instruction.Operand;
                    ilCode.AppendLine(instructionText);
                }
                return ilCode.ToString();
                /*
                        if (instruction.Operand != null)
                        {
                        
                            if (instruction.Operand.ToString() == methodToFind)
                            {
                                var sourceMethod = (MethodDefinition) instruction.Operand;
                                // DI.log.debug("[{0}] {1} ", instruction.OpCode.Name,

                                var sinkMethod =
                                    (MethodDefinition) methodDefinition.Body.Instructions[i - parameterOffset].Operand;
                                // DI.log.debug("-->[{0}] {1} ", instructionWithParameter.OpCode.Name,
                                //               instructionWithParameter.Operand.ToString());
                                // DI.log.debug("{0} -- > {1}", sourceMethod.Name, sinkMethod.Name);
                                //MethodDefinition property = (MethodDefinition)method.Body.Instructions[i - parameterOffset].Operand;
                                findings.Add(String.Format("{0} -- > {1}", sourceMethod.Name, sinkMethod.Name));
                            }
                        }*/

                //        return ilCode.ToString();
                //            return "raw Il Parsing";
            }
            catch (Exception ex)
            {
                DI.log.error("In getIL_usingRawIlParsing :{0} \n\n {1} \n\n", ex.Message, ex.StackTrace);
            }
            return "";
        }


        public string getIL_usingControlFLowGraph(MethodDefinition method)
        {
            try
            {
                ControlFlowGraph cfg = ControlFlowGraph.Create(method);

                string ilCode = FormatControlFlowGraph(cfg);
                return ilCode;
            }
            catch (Exception ex)
            {
                DI.log.error("in CecilDecompiler.getIL :{0} \n\n{1}\n\n", ex.Message, ex.StackTrace);
            }
            return "";
        }

        public string FormatControlFlowGraph(ControlFlowGraph cfg)
        {
            //var memoryStream = new MemoryStream();
            var stringWriter = new StringWriter();
            foreach (InstructionBlock block in cfg.Blocks)
            {
                stringWriter.WriteLine("block {0}:", block.Index);
                stringWriter.WriteLine("\tbody:");
                foreach (Instruction instruction in block)
                {
                    stringWriter.Write("\t\t");
                    InstructionData data = cfg.GetData(instruction);
                    stringWriter.Write("[{0}:{1}] ", data.StackBefore, data.StackAfter);
                    Formatter.WriteInstruction(stringWriter, instruction);
                    stringWriter.WriteLine();
                }
                InstructionBlock[] successors = block.Successors;
                if (successors.Length > 0)
                {
                    stringWriter.WriteLine("\tsuccessors:");
                    foreach (InstructionBlock successor in successors)
                    {
                        stringWriter.WriteLine("\t\tblock {0}", successor.Index);
                    }
                }
            }
            //memoryStream.Flush();
            //writer.Flush();
            return stringWriter.ToString();
            // return Encoding.ASCII.GetString(memoryStream.ToArray());
            //return writer.ToString();
        }


        public string getILfromClonedMethod(MethodDefinition methodDefinition)
        {
            return "Not working at the moment";
            //MethodDefinition clonedMethod = methodDefinition.Clone();
            //return getIL(clonedMethod);


            //return getIL_usingRawIlParsing(methodDefinition);
            //return "this is cloned IL";
        }


        public void decompile(AssemblyDefinition assemblyDefinition, string pathToSaveDecompiledSourceCode)
        {
     //       Files.checkIfDirectoryExistsAndCreateIfNot(pathToSaveDecompiledSourceCode);
            //var methodIndex =  
            try
            {
                foreach (var typeDefinition in MonoCecil.CecilUtils.getTypes(assemblyDefinition))
                {
                    DI.log.alsoShowInConsole = true;
                    var sourceCodeContents = new StringBuilder();
                    foreach (var method in MonoCecil.CecilUtils.getMethods((typeDefinition)))
                    {
                        DI.log.info("[{0}]decompiling method: {1}",sourceCodeContents.Length, method.ToString());
                        sourceCodeContents.AppendLine(getSourceCode(method));
                    }
                    if (sourceCodeContents.Length > 0)
                    {
                        var targetFile = Path.Combine(pathToSaveDecompiledSourceCode, Files.getSafeFileNameString(typeDefinition.Name) + ".cs");
                        Files.WriteFileContent(targetFile, sourceCodeContents.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                DI.log.error("in decompile: {0}", ex.Message);
            }
        }
    }
}
