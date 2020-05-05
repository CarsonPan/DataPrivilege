grammar DataPrivilege;
comparisonOperator
    : '=' 
    | '>' 
    | '<' 
    | '<' '=' 
    | '>' '=' 
    | '<' '>' 
    | '!' '=' 
    | '!' '>' 
    | '!' '<'
    ;

expression
    :columnElem
    |constantExpression
    |customField
    |getDateExpression
    ;

expressionList
    : expression (',' expression)*
    ;

columnElem
    : (ID '.')? ID
    ;

simpleSubquery
    : SELECT
      columnElem
      FROM tableName
      (WHERE where=searchCondition)?
    ;
tableName
    :ID AS? ID?
    ;

searchConditionAnd
    : searchConditionNot (AND searchConditionNot)*
    ;

searchConditionNot
    : NOT? predicate
    ;

searchCondition
    : searchConditionAnd (OR searchConditionAnd)*
    ;

customField
    :'{' ID '}'
    ;

numericExpression
    :('+'|'-')? DECIMAL
    |BINARY
    |('+'|'-')? (REAL | FLOAT) 
    ;
stringExpression
    :STRING
    ;
nullExpression
    :NULL
    ;
constantExpression
    : stringExpression 
    | numericExpression
    | nullExpression
    ;

getDateExpression
    :GETDATE
    ;

existsExpression
    :NOT? EXISTS '(' simpleSubquery ')'
    ;

comparisonExpression
    :expression comparisonOperator expression
    ;

betweenAndExpression
    :expression NOT? BETWEEN expression AND expression
    ;

inExpression
    :expression NOT? IN '(' (simpleSubquery | expressionList) ')'
    ;

likeExpression
    :expression NOT? LIKE STRING
    ;

isNullExpression
    :expression IS NOT? NULL
    ;
 
predicate
    : existsExpression
    | comparisonExpression 
    | betweenAndExpression
    | inExpression
    | likeExpression
    | isNullExpression
    | '(' searchCondition ')'
    ;

//lexer
LINECOMMENT:'--' .*? '\n' ->skip;
COMMENT:'/* '.*? '*/'     ->skip;
AND:                                   [Aa][Nn][Dd];
OR:                                    [Oo][rR];
ASC:                                   [aA][sS][cC];
DESC:                                  [dD][eE][sS][cC];
BETWEEN:                               [Bb][eE][tT][wW][eE][eE][nN];
EXISTS:                                [eE][xX][Ii][sS][Tt][sS];
IN:                                    [iI][nN];
IS:                                    [iI][sS];
NULL:                                  [nN][uU][lL][lL];
LIKE:                                  [lL][iI][kK][eE];
NOT:                                   [nN][oO][tT];
//TOP:                                   [tT][oO][pP];
WHERE:                                 [wW][hH][eE][rR][eE];
SELECT:                                [sS][eE][lL][eE][cC][tT];
FROM:                                  [fF][rR][oO][mM];
ORDER:                                 [oO][rR][dD][eE][rR];
BY:                                    [bB][yY];
GETDATE:                               [gG][eE][tT][dD][aA][tT][eE]'(' ')';
EQUAL:               '=';
GREATER:             '>';
LESS:                '<';
EXCLAMATION:         '!';
DOT:                 '.';
ID: [a-zA-Z_][a-zA-Z0-9_]*;   
AS:[aA][sS];
STRING:              'N'? '\'' (~'\'' | '\'\'')* '\'';
DECIMAL:             DEC_DIGIT+;
BINARY:              '0' ('X'|'x') HEX_DIGIT*;
FLOAT:               DEC_DOT_DEC;
REAL:                (DECIMAL | DEC_DOT_DEC) (('E'|'e') [+-]? DEC_DIGIT+);
S:[ \t\r\n]->skip;
fragment DEC_DOT_DEC:  (DEC_DIGIT+ '.' DEC_DIGIT+ |  DEC_DIGIT+ '.' | '.' DEC_DIGIT+);
fragment HEX_DIGIT:    [0-9A-Fa-f];
fragment DEC_DIGIT:    [0-9];