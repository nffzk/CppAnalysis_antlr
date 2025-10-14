using System;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using CppParser.Grammars.Generated;
using CppParser.Models;

namespace CppParser.Services
{
    public sealed class HeaderParser
    {
        public sealed class ParserOptions
        {
            public bool EnableSllThenLlFallback { get; set; } = true;
            public bool UseBailErrorStrategy { get; set; } = true;
            public int? TokenThreshold { get; set; } = null;
        }

        public Antlr4.Runtime.Tree.IParseTree Parse(string source, ParserOptions? options, out CPP14Parser parser)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            options ??= new ParserOptions();

            var input = new AntlrInputStream(source);
            var lexer = new CPP14Lexer(input);
            var tokens = new CommonTokenStream(lexer);
            parser = new CPP14Parser(tokens);

            if (options.UseBailErrorStrategy)
                parser.ErrorHandler = new BailErrorStrategy();

            if (options.TokenThreshold is int limit)
            {
                tokens.Fill();
                if (tokens.Size > limit)
                    throw new InvalidOperationException($"Token count {tokens.Size} exceeds threshold {limit}.");
            }

            if (options.EnableSllThenLlFallback)
            {
                parser.Interpreter.PredictionMode = PredictionMode.SLL;
                try { return parser.translationUnit(); }
                catch
                {
                    tokens.Seek(0);
                    parser.Reset();
                    parser.ErrorHandler = new DefaultErrorStrategy();
                    parser.Interpreter.PredictionMode = PredictionMode.LL;
                    return parser.translationUnit();
                }
            }

            parser.Interpreter.PredictionMode = PredictionMode.LL;
            return parser.translationUnit();
        }

        public CppHeaderFile BuildHeaderModel(string fileName, string source, ParserOptions? options = null)
        {
            var tree = Parse(source, options, out _);
            var builder = new HeaderModelBuilder(fileName);
            var result = builder.Visit(tree) as CppHeaderFile;
            return result ?? new CppHeaderFile { FileName = fileName };
        }
    }
}
