﻿namespace GraphQLCore.Type.Directives
{
    using GraphQLCore.Type.Scalar;
    using GraphQLCore.Type.Translation;
    using Language.AST;
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    public class GraphQLSkipDirectiveType : GraphQLDirectiveType
    {
        public GraphQLSkipDirectiveType()
            : base(
                "skip",
                "Directs the executor to skip this field or fragment when the `if` argument is true.",
                DirectiveLocation.FIELD,
                DirectiveLocation.FRAGMENT_SPREAD,
                DirectiveLocation.INLINE_FRAGMENT)
        {
            this.Argument("if").WithDescription("Skipped when true.");
        }

        public override bool PreExecutionIncludeFieldIntoResult(
            GraphQLDirective directive,
            ISchemaRepository schemaRepository)
        {
            var argument = directive.Arguments.Single(e => e.Name.Value == "if");
            var booleanType = new GraphQLBoolean();

            var result = booleanType.GetFromAst(argument.Value, schemaRepository);

            return !(bool)result.Value;
        }

        public override LambdaExpression GetResolver(Func<Task<object>> valueGetter, object parentValue)
        {
            Expression<Func<bool, object>> resolver = (@if) => valueGetter().Result;

            return resolver;
        }
    }
}