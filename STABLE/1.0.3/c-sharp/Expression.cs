/*
 * @(#)Expression.cs        1.0.3   2015-12-15
 * 
 * You may use this software under the condition of "Simplified BSD License"
 * 
 * Copyright 2010 MARIUSZ GROMADA. All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without modification, are
 * permitted provided that the following conditions are met:
 * 
 *    1. Redistributions of source code must retain the above copyright notice, this list of
 *       conditions and the following disclaimer.
 * 
 *    2. Redistributions in binary form must reproduce the above copyright notice, this list
 *       of conditions and the following disclaimer in the documentation and/or other materials
 *       provided with the distribution.
 * 
 * THIS SOFTWARE IS PROVIDED BY <MARIUSZ GROMADA> ``AS IS'' AND ANY EXPRESS OR IMPLIED
 * WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
 * FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
 * ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 * 
 * The views and conclusions contained in the software and documentation are those of the
 * authors and should not be interpreted as representing official policies, either expressed
 * or implied, of MARIUSZ GROMADA.
 * 
 * If you have any questions/bugs feel free to contact:
 * 
 *     Mariusz Gromada
 *     mariusz.gromada@mathspace.pl
 *     http://mathspace.pl/
 *     http://mxparser.sourceforge.net/
 * 
 *                              Asked if he believes in one God, a mathematician answered: 
 *                              "Yes, up to isomorphism."  
 */ 


using System;
using System.IO; 
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.Generic;
using org.mariuszgromada.math.mxparser.mathcollection;
using org.mariuszgromada.math.mxparser.syntaxchecker;


namespace org.mariuszgromada.math.mxparser {


    /**
     * Expression - base class for real expressions definition.
     * 
     * Examples:
     * <ul>
     * <li>'1+2'
     * <li>'sin(x)+1'
     * <li>'asin(3*x)^10-log(4,8)'
     * <li>in general 'f(x1,x2,...,xn)' where x1,...,xn are real
     * arguments
     * </ul>
     * <p>
     * Class provides easy way to define multivariate arithmetic expression.
     * 
     * 
     * @author         <b>Mariusz Gromada</b><br/>
     *                 <a href="mailto:mariusz.gromada@mathspace.pl">mariusz.gromada@mathspace.pl</a><br>
     *                 <a href="http://mathspace.pl/">http://mathspace.pl/</a><br>
     *                 <a href="http://mxparser.sourceforge.net/">http://mxparser.sourceforge.net/</a><br>
     *                         
     * @version        1.0
     * 
     * @see            Argument
     * @see            RecursiveArgument
     * @see            Constant
     * @see            Function  
     */
    public class Expression {



	    public static void wait (int n){
            long t0,t1;
            t0=DateTime.Now.Millisecond;
            do{
                t1=DateTime.Now.Millisecond;
            }
            while (t1-t0<1000);
	    }

    	
	    /**
	     * mXparser version
	     */
	    internal const String VERSION = "1.0";
    	
	    /**
	     * FOUND / NOT_FOUND
	     * used for matching purposes
	     */
	    internal const int NOT_FOUND = -1;
	    internal const int FOUND = 0;
    	
	    /**
	     * Marker for internal processing
	     */
	    internal const bool INTERNAL = true;
    	
    	
	    /**
	     * For verbose mode purposes
	     */
	    private const bool WITH_EXP_STR = true;
	    private const bool NO_EXP_STR = false;
    		
	    /**
	     * Status of the Expression syntax
	     */	
	    public const bool NO_SYNTAX_ERRORS = true;
	    public const bool SYNTAX_ERROR_OR_STATUS_UNKNOWN = false;
    	
    	
	    /**
	     * Expression string (for example: "sin(x)+cos(y)")
	     */	
	    internal String expressionString;
	    private String description;
    	
	    /**
	     * List of arguments
	     * 
	     * @see        Argument
	     * @see        RecursiveArgument
	     */	
	    internal List<Argument> argumentsList;
    	
	    /**
	     * List of user defined functions
	     * 
	     * @see        Function
	     */		
	    internal List<Function> functionsList;

	    /**
	     * List of user defined constants
	     * 
	     * @see        Constant
	     */		
	    internal List<Constant> constantsList;
    	
    	
	    /**
	     * List of key words known by the parser
	     */		
	    private List<KeyWord> keyWordsList;
    	
	    /**
	     * List of expression tokens (words).
	     * Token class defines all needed
	     * attributes for recognizing the structure of
	     * arithmetic expression. This is the key result when
	     * initial parsing is finished (tokenizeExpressionString() - method).
	     * Token keeps information about:
	     *    - token type (for example: function, operator, argument, number, etc...)
	     *    - token identifier within given type (sin, cos, operaotr, etc...)
	     *    - token value (if token is a number)
	     *    - token level - key information regarding sequence (order) of further parsing 
	     */				
	    private List<Token> initialTokens;
    	
	    /**
	     * the initialTokens list keeps unchanged information about
	     * found tokens.
	     * 
	     * While parsing the tokensList is used. The tokensList is the same
	     * as initialTokens list at the beginning of the calculation process.
	     * Each math operation changes tokens list - it means that
	     * tokens are parameters when performing math operation
	     * and the result is also presented as token (usually as a number token)
	     * At the end of the calculation the tokensList should contain only one
	     * element - the result of all calculations. 
	     */
	    private List<Token> tokensList;
    	
	    /**
	     * List of related expressions, for example when
	     * user defined function is used in the expression
	     * or dependent argument was defined. Modification of 
	     * function expression calls the method expression modified
	     * flag method to all related expressions.
	     * 
	     * Related expression usually are used for
	     * - dependent arguments
	     * - recursive arguments
	     * - user functions
	     */
	    internal List<Expression> relatedExpressionsList;	
    	
	    /**
	     * Keeps computing time
	     */
	    private double computingTime;
    	
	    /**
	     * if true then new tokenizing is required
	     * (the initialTokens list needs to be updated)
	     */
	    private bool expressionWasModified;
    	
	    /**
	     * If recursive mode is on the recursive calls are permitted.
	     * It mean there will be no null pointer exceptions
	     * due to expression, and functions cloning.
	     */
	    internal bool recursiveMode;
    	
	    /**
	     * Verbose mode prints processing info
	     * calls System.out.print* methods
	     */
	    private bool verboseMode;

	    /**
	     * Status of the expression syntax
	     * 
	     * Please referet to the:
	     *    - NO_SYNTAX_ERRORS
	     *    - SYNTAX_ERROR_OR_STATUS_UNKNOWN
	     */
	    private bool syntaxStatus;
    	
	    /**
	     * Message after checking the syntax
	     */
	    private String errorMessage;


        /**
         * Flag used internally to mark started recursion
         * call on the current object, necessary to
         * avoid infinite loops while recursive syntax
         * checking (i.e. f -> g and g -> f)
         * or marking modified flags on the expressions
         * related to this expression.
         * 
         * @see setExpressionModifiedFlag()
         * @see checkSyntax()
         */
        private bool recursionCallPending;

        /*=================================================
	     * 
	     * Related expressions handling
	     * 
	     *================================================= 
	     */


        /**
	     * Adds related expression
	     * The same expression could be added more than once
	     * For example when 
	     * 
	     * @param      expression          the expression
	     */
        internal void addRelatedExpression(Expression expression) {

		    if ((expression != null) && (expression != this))
			    if ( !relatedExpressionsList.Contains(expression))
				    relatedExpressionsList.Add(expression);

	    }
    	
	    /**
	     * Removes related expression
	     * 
	     * @param      expression          the expression
	     */
	    internal void removeRelatedExpression(Expression expression) {

		    relatedExpressionsList.Remove(expression);

	    }
    	
    	
	    /**
	     * Prints related expression list
	     */
	    internal void showRelatedExpressions() {

		    mXparser.consolePrintln();
		    mXparser.consolePrintln(this.description + " = " + this.expressionString + ":");
		    foreach (Expression e in relatedExpressionsList)
			    mXparser.consolePrintln("-> " + e.description + " = " + e.expressionString);

	    }


	    /**
	     * Method return error message after
	     * calling checkSyntax() method or
	     * calculate().
	     * 
	     * @return     Error message as string.
	     */
	    public String getErrorMessage() {

		    return errorMessage;

	    }
    	

	    /**
	     * Gets syntax status of the expression.
	     * 
	     * @return     true if there are no syntax errors,
	     *             false when syntax error was found or
	     *             syntax status is unknown
	     */
	    public bool getSyntaxStatus() {

		    return this.syntaxStatus;

	    }
    	
    	
	    /**
	     * Sets expression status to modified
	     * Calls setExpressionModifiedFlag() method
	     * to all related expressions.
	     */
	    internal void setExpressionModifiedFlag() {
            if (recursionCallPending == false) {
                recursionCallPending = true;
                expressionWasModified = true;
                syntaxStatus = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
                errorMessage = "Syntax status unknown.";

                foreach (Expression e in relatedExpressionsList)
                    e.setExpressionModifiedFlag();

                recursionCallPending = false;
            }
        }

        /**
         * Common variables while expression initializing
         */
        private void expressionInternalVarsInit() {
            description = "";
            errorMessage = "";
            computingTime = 0;
            recursionCallPending = false;
        }

        /**
	     * Common elements while expression initializing
	     */
        private void expressionInit() {

		    /*
		     * New lists
		     */
		    argumentsList = new List<Argument>();
		    functionsList = new List<Function>();
		    constantsList = new List<Constant>();
		    relatedExpressionsList = new List<Expression>();
    		
		    /*
		     * Empty description
		     * Silent mode
		     * No recursive mode
		     */
		    setSilentMode();
		    disableRecursiveMode();
            expressionInternalVarsInit();
        }

        /*=================================================
	     * 
	     * Constructors
	     * 
	     *================================================= 
	     */



        /**
	     * Default constructor - empty expression
	     */
        public Expression() {
		    expressionString = "";
		    expressionInit();
		    setExpressionModifiedFlag();
	    }
    		
    	
	    /**
	     * Constructor - creates new expression from expression string.
	     * 
	     * @param      expressionString    definition of the expression 
	     */	
	    public Expression(String expressionString) {
		    expressionInit();
		    this.expressionString = String.Copy(expressionString);
		    setExpressionModifiedFlag();
	    }
    	
	    /**
	     * Constructor - creates new expression from expression string
	     * and arguments list.
	     * 
	     * @param      expressionString    the expression string
	     * @param      argumentsList       the arguments list
	     * 
	     * @see        Argument
	     * @see        RecursiveArgument
	     */
	    public Expression(String expressionString,
			    List<Argument> argumentsList) {

		    this.expressionString = String.Copy(expressionString);

		    functionsList = new List<Function>();
		    constantsList = new List<Constant>();
		    relatedExpressionsList = new List<Expression>();
            expressionInternalVarsInit();
            setSilentMode();
		    disableRecursiveMode();

		    this.argumentsList = argumentsList;
		    foreach (Argument a in argumentsList)
			    a.addRelatedExpression(this);

		    setExpressionModifiedFlag();

	    }

	    /**
	     * Constructor - creates new expression from expression string,
	     * arguments list and functions list.
	     *  
	     * @param      expressionString    the expression string
	     * @param      argumentsList       the arguments list
	     * @param      functionsList       the functions list
	     * 
	     * @see        Argument
	     * @see        RecursiveArgument
	     * @see        Function
	     */
	    public Expression(String expressionString, List<Argument> argumentsList,
			    List<Function> functionsList) {
     
		    this.expressionString = String.Copy(expressionString);

		    constantsList = new List<Constant>();
		    relatedExpressionsList = new List<Expression>();
            expressionInternalVarsInit();
            setSilentMode();
		    disableRecursiveMode();

		    this.argumentsList = argumentsList;
		    foreach (Argument a in argumentsList)
			    a.addRelatedExpression(this);

		    this.functionsList = functionsList;
		    foreach (Function f in functionsList)
			    f.addRelatedExpression(this);

		    setExpressionModifiedFlag();

	    }

	    /**
	     * Constructor - creates new expression from expression string,
	     * arguments, functions list and constants list.
	     * 
	     * @param      expressionString    the expression string
	     * @param      argumentsList       the arguments list
	     * @param      functionsList       the functions list
	     * @param      constantsList       the constant list
	     * 
	     * @see        Argument
	     * @see        RecursiveArgument
	     * @see        Function
	     * @see        Constant
	     */
	    public Expression(String expressionString, List<Argument> argumentsList,
			    List<Function> functionsList, List<Constant> constantsList) {

		    this.expressionString = String.Copy(expressionString);

		    relatedExpressionsList = new List<Expression>();
            expressionInternalVarsInit();
            setSilentMode();
		    disableRecursiveMode();

		    this.argumentsList = argumentsList;
		    foreach (Argument a in argumentsList)
			    a.addRelatedExpression(this);

		    this.functionsList = functionsList;
		    foreach (Function f in functionsList)
			    f.addRelatedExpression(this);

		    this.constantsList = constantsList;
		    foreach (Constant c in constantsList)
			    c.addRelatedExpression(this);

		    setExpressionModifiedFlag();

	    }

	    /**
	     * Constructor - creates new expression from expression string,
	     * and list of arguments given as variadic parameters list.
	     * 
	     * For example:
	     *    Argument x1 = new Argument("x1");
	     *    Argument x2 = new Argument("x2");
	     *    Expression exp = new Expression("x1+x2", x1, x2);
	     * 
	     * @param      expressionString      definition of the expression
	     * @param      arguments             arguments list (comma separated)
	     * 
	     * @see        Argument
	     */	
	    public Expression(String expressionString, params Argument[] arguments) {
    		
		    expressionInit();
		    this.expressionString = String.Copy(expressionString);
		    foreach (Argument arg in arguments) {
			    arg.addRelatedExpression(this);
			    argumentsList.Add(arg);
		    }

		    setExpressionModifiedFlag();

	    }
    	
    	

	    /**
	     * Package level constructor - creates new expression from subexpression
	     * (sublist of the tokens list), arguments list, functions list and
	     * constants list (used by the internal calculus operations, etc...).
	     * 
	     * @param      expressionString    the expression string
	     * @param      initialTokens       the tokens list (starting point - no tokenizing,
	     *                                 no syntax checking)
	     * @param      argumentsList       the arguments list
	     * @param      functionsList       the functions list
	     * @param      constantsList       the constants list
	     */
	    internal Expression(String expressionString, List<Token> initialTokens, List<Argument> argumentsList,
			    List<Function> functionsList, List<Constant> constantsList) {

		    this.expressionString = String.Copy(expressionString);
		    this.initialTokens = initialTokens;
		    this.argumentsList = argumentsList;		
		    this.functionsList = functionsList;
		    this.constantsList = constantsList;
		    relatedExpressionsList = new List<Expression>();	

		    expressionWasModified = false;
		    syntaxStatus = NO_SYNTAX_ERRORS;
		    description = "_internal_";
		    errorMessage = "";
		    computingTime = 0;
            recursionCallPending = false;

            setSilentMode();
		    disableRecursiveMode();		


	    }
    	
	    /**
	     * Package level constructor - creates new expression from expression string,
	     * arguments list, functions list and constants list (used by the
	     * RecursiveArgument class).
	     * 
	     * No related expressions at the beginning.
	     *  
	     * @param      expressionString    the expression string
	     * @param      argumentsList       the arguments list
	     * @param      functionsList       the functions list
	     * @param      constantsList       the constants list
	     * @param      internal            the marker for internal processing
	     * 
	     * @see        Argument
	     * @see        RecursiveArgument
	     * @see        Function
	     * @see        Constant
	     */
	    internal Expression(String expressionString, List<Argument> argumentsList,
			    List<Function> functionsList, List<Constant> constantsList
			    ,bool i) {

		    this.expressionString = String.Copy(expressionString);

            expressionInternalVarsInit();
            setSilentMode();
		    disableRecursiveMode();

		    this.argumentsList = argumentsList;		
		    this.functionsList = functionsList;
		    this.constantsList = constantsList;
		    relatedExpressionsList = new List<Expression>();	

		    setExpressionModifiedFlag();

	    }
    	
	    /**
	     * Private constructor - expression cloning.
	     * 
	     * @param      expression          the base expression
	     */
	    private Expression(Expression expression) {
    		
		    expressionString = String.Copy(expression.expressionString);
		    description = String.Copy(expression.description);
		    argumentsList = expression.argumentsList;
		    functionsList = expression.functionsList;
		    constantsList = expression.constantsList;
		    keyWordsList = expression.keyWordsList;
		    relatedExpressionsList = expression.relatedExpressionsList;
		    computingTime = 0;
		    expressionWasModified = expression.expressionWasModified;
		    recursiveMode = expression.recursiveMode;
		    verboseMode = expression.verboseMode;
		    syntaxStatus = expression.syntaxStatus;
		    errorMessage = String.Copy(expression.errorMessage);
            recursionCallPending = expression.recursionCallPending;

        }


        /**
         * Sets (modifies expression) expression string.
         * 
         * @param      expressionString    the expression string
         */
        public void setExpressionString(String expressionString) {
    	
			    this.expressionString = expressionString;
    			
			    setExpressionModifiedFlag();
    	
		    }
    		
    		
		    /**
		     * Returns expression string
		     */
		    public String getExpressionString() {
    	
			    return expressionString;
    	
		    }
    		
		    /**
		     * Clears expression string
		     */	
		    public void clearExpressionString() {
    	
			    this.expressionString = "";
			    setExpressionModifiedFlag();
    	
		    }
    		
		    /**
		     * Sets expression description.
		     * 
		     * @param      description         the description string
		     */
		    public void setDescription(String description) {
    	
			    this.description = description;
    	
		    }
    	
		    /**
		     * Gets expression description.
		     * 
		     * @return     String description.
		     */
		    public String getDescription() {
    	
			    return description;
    	
		    }
    		
		    /**
		     * Clears expression description
		     */	
		    public void clearDescription() {
    	
			    this.description = "";
    	
		    }
    		
		    /**
		     * Enables verbose mode.
		     */
		    public void setVerboseMode() {
    	
			    verboseMode = true;
    	
		    }
    		
		    /**
		     * Disables verbose mode (default silent mode).
		     */
		    public void setSilentMode() {
    	
			    verboseMode = false;
    	
		    }
    		
		    /**
		     * Returns verbose mode status.
		     * 
		     * @return     true if verbose mode is on,
		     *             otherwise returns false.
		     */
		    public bool getVerboseMode() {
    			
			    return verboseMode;
    			
		    }
    		
    		
		    /**
		     * Sets recursive mode
		     */
		    public void setRecursiveMode() {
    	
			    recursiveMode = true;
    	
		    }
    		
		    /**
		     * Disables recursive mode 
		     */
		    public void disableRecursiveMode() {
    	
			    recursiveMode = false;
    	
		    }
    		
		    /**
		     * Gets recursive mode status
		     * 
		     * @return     true if recursive mode is enabled,
		     *             otherwise returns false.
		     */
		    public bool getRecursiveMode() {
    	
			    return recursiveMode;
    	
		    }
    		
		    /**
		     * Gets computing time.
		     * 
		     * @return     computing time in seconds.
		     */	
		    public double getComputingTime() {
    	
			    return computingTime;
    	
		    }
    	
		    /*=================================================
		     * 
		     * Arguments handling API
		     * 
		     *================================================= 
		     */
    	
    	
		    /**
		     * Adds arguments (variadic) to the expression definition.
		     * 
		     * @param      arguments           the arguments list
		     *                                 (comma separated list)
		     * @see        Argument
		     * @see        RecursiveArgument
		     */
		    public void addArguments(params Argument[] arguments) {
    			
			    foreach (Argument arg in arguments) {
				    argumentsList.Add(arg);
				    arg.addRelatedExpression(this);
			    }
    			
			    setExpressionModifiedFlag();		
    	
		    }
    	
		    /**
		     * Adds arguments to the expression definition.
		     * 
		     * @param      argumentsList       the arguments list
		     * 
		     * @see        Argument
		     * @see        RecursiveArgument
		     */
		    public void addArguments( List<Argument> argumentsList) {

			    this.argumentsList.AddRange( argumentsList );
    			
			    foreach (Argument arg in argumentsList)
				    arg.addRelatedExpression(this);
    			
			    setExpressionModifiedFlag();		
    	
		    }	
    			
		    /**
		     * Enables to define the arguments (associated with
		     * the expression) based on the given arguments names.
		     * 
		     * @param      argumentsNames      the arguments names (variadic)
		     *                                 comma separated list
		     *                       
		     * @see        Argument
		     * @see        RecursiveArgument
		     */
		    public void defineArguments(params String[] argumentsNames) {
    	
			    foreach (String argName in argumentsNames) {
				    Argument arg = new Argument(argName);
				    arg.addRelatedExpression(this);
				    argumentsList.Add(arg);
			    }
			    setExpressionModifiedFlag();		
    	
		    }
    		
    		
		    /**
		     * Enables to define the argument (associated with the expression)
		     * based on the argument name and the argument value.
		     * 
		     * @param      argumentName        the argument name
		     * @param      argumentValue       the the argument value
		     * 
		     * @see        Argument
		     * @see        RecursiveArgument
		     */
		    public void defineArgument(String argumentName, double argumentValue) {
    	
			    Argument arg = new Argument(argumentName, argumentValue);
			    arg.addRelatedExpression(this);
			    argumentsList.Add(arg);
			    setExpressionModifiedFlag();		
    	
		    }
    		
		    /**
		     * Gets argument index from the expression. 
		     * 
		     * @param      argumentName        the argument name
		     * 
		     * @return     The argument index if the argument name was found,
		     *             otherwise returns Argument.NOT_FOUND
		     *                       
		     * @see        Argument
		     * @see        RecursiveArgument
		     */
		    public int getArgumentIndex(String argumentName) {
    			
			    int argumentsNumber = argumentsList.Count;
    			
			    if (argumentsNumber > 0) {
    				
				    int argumentIndex = 0;
				    int searchResult = NOT_FOUND; 
    				
				    while ((argumentIndex < argumentsNumber)&&(searchResult == NOT_FOUND)) {
					    if (argumentsList[argumentIndex].getArgumentName().Equals(argumentName))
						    searchResult = FOUND;
					    else
						    argumentIndex++;
				    }
    				
				    if (searchResult == FOUND)
					    return argumentIndex;
				    else
					    return NOT_FOUND;
    				
    				
			    } else 
				    return NOT_FOUND;
    			
		    }
    		
		    /**
		     * Gets argument from the expression. 
		     * 
		     * 
		     * @param      argumentName        the argument name
		     * 
		     * @return     The argument if the argument name was found,
		     *             otherwise returns null.
		     *                       
		     * @see        Argument
		     * @see        RecursiveArgument
		     */
		    public Argument getArgument(String argumentName) {
    			
			    int argumentIndex = getArgumentIndex(argumentName);
    			
			    if (argumentIndex == NOT_FOUND)
				    return null;
			    else
				    return argumentsList[argumentIndex];
    			
		    }
    		
		    /**
		     * Gets argument from the expression.
		     * 
		     * @param      argumentIndex       the argument index
		     * 
		     * @return     Argument if the argument index is between 0 and 
		     *             the last available argument index (getArgumentsNumber()-1),
		     *             otherwise returns null.
		     *                        
		     * @see        Argument
		     * @see        RecursiveArgument
		     */
		    public Argument getArgument(int argumentIndex) {
    			
			    if ( (argumentIndex < 0) || (argumentIndex >= argumentsList.Count) )
				    return null;
			    else
				    return argumentsList[argumentIndex];
		    }
    		
		    /**
		     * Gets number of arguments associated with the expression.
		     * 
		     * @return     The number of arguments (int >= 0)
		     * 
		     * @see        Argument
		     * @see        RecursiveArgument
		     */
		    public int getArgumentsNumber() {
    	
			    return argumentsList.Count;
    	
		    }
    		
		    /**
		     * Sets argument value.
		     * 
		     * @param      argumentName        the argument name
		     * @param      argumentValue       the argument value
		     */
		    public void setArgumentValue(String argumentName, double argumentValue) {
    	
			    int argumentIndex = getArgumentIndex(argumentName);
    			
			    if (argumentIndex != NOT_FOUND)
				    argumentsList[argumentIndex].setArgumentValue(argumentValue);
    	
		    }
    		
		    /**
		     * Gets argument vale.
		     * 
		     * @param      argumentName        the argument name
		     * 
		     * @return     Argument value if argument name was found,
		     *             otherwise return Double.NaN.
		     */
		    public double getArgumentValue(String argumentName) {
    			
			    int argumentIndex = getArgumentIndex(argumentName);
    			
			    if (argumentIndex != NOT_FOUND)
				    return argumentsList[argumentIndex].getArgumentValue();
			    else
				    return Double.NaN;
    	
		    }
    	
		    /**
		     * Removes first occurrences of the arguments
		     * associated with the expression.
		     * 
		     * @param      argumentsNames      the arguments names
		     *                                 (variadic parameters) comma separated
		     *                                 list
		     * 
		     * @see        Argument
		     * @see        RecursiveArgument
		     */
		    public void removeArguments(params String[] argumentsNames) {
    	
			    foreach (String argumentName in argumentsNames) {
    				
				    int argumentIndex = getArgumentIndex(argumentName);
    				
				    if (argumentIndex != NOT_FOUND) {
					    Argument arg = argumentsList[argumentIndex];
					    arg.removeRelatedExpression(this);
					    argumentsList.RemoveAt(argumentIndex);
				    }
    				
			    }
    			
			    setExpressionModifiedFlag();
    			
		    }
    	
		    /**
		     * Removes first occurrences of the arguments
		     * associated with the expression.
		     * 
		     * @param      arguments           the arguments (variadic parameters)
		     *                                 comma separated list
		     * 
		     * @see        Argument
		     * @see        RecursiveArgument
		     */
		    public void removeArguments(params Argument[] arguments) {
    	
    			
			    foreach (Argument argument in arguments) {
    	
				    argumentsList.Remove(argument);
				    argument.removeRelatedExpression(this);
    			
			    }
    			
			    setExpressionModifiedFlag();		
    	
		    }	
    		
		    /**
		     * Removes all arguments associated with the expression.
		     * 
		     * @see        Argument
		     * @see        RecursiveArgument
		     */
		    public void removeAllArguments() {
    	
			    foreach (Argument arg in argumentsList)
				    arg.removeRelatedExpression(this);
    			
			    argumentsList.Clear();
			    setExpressionModifiedFlag();		
    	
		    }
    			
		    /*=================================================
		     * 
		     * Constants handling API
		     * 
		     *================================================= 
		     */
    	
    		
		    /**
		     * Adds constants (variadic parameters) to the expression definition.
		     * 
		     * @param      constants           the constants
		     *                                 (comma separated list)
		     *                    
		     * @see        Constant
		     */
		    public void addConstants(params Constant[] constants) {
    	
			    foreach (Constant constant in constants) {
				    constantsList.Add(constant);
				    constant.addRelatedExpression(this);
			    }	
    	
			    setExpressionModifiedFlag();
    	
		    }
    	
    		
		    /**
		     * Adds constants to the expression definition.
		     * 
		     * @param      constantsList       the list of constants
		     * 
		     * @see        Constant
		     */
		    public void addConstants( List<Constant> constantsList) {
    	
			    this.constantsList.AddRange( constantsList );
    	
			    foreach (Constant c in constantsList)
				    c.addRelatedExpression(this);
    			
			    setExpressionModifiedFlag();		
    	
		    }	
    		
		    /**
		     * Enables to define the constant (associated with 
		     * the expression) based on the constant name and
		     * constant value.
		     * 
		     * @param      constantName        the constant name
		     * @param      constantValue       the constant value
		     * 
		     * @see        Constant
		     */
		    public void defineConstant(String constantName, double constantValue) {
    	
			    Constant c = new Constant(constantName, constantValue);
			    c.addRelatedExpression(this);
			    constantsList.Add(c);
			    setExpressionModifiedFlag();		
    	
		    }
    		
		    /**
		     * Gets constant index associated with the expression.
		     * 
		     * @param      constantName        the constant name
		     * 
		     * @return     Constant index if constant name was found,
		     *             otherwise return Constant.NOT_FOUND.
		     * 
		     * @see        Constant
		     */
		    public int getConstantIndex(String constantName) {
    			
			    int constantsNumber = constantsList.Count;
    			
			    if (constantsNumber > 0) {
    				
				    int constantIndex = 0;
				    int searchResult = NOT_FOUND; 
    				
				    while ((constantIndex < constantsNumber)&&(searchResult == NOT_FOUND)) {
					    if (constantsList[constantIndex].getConstantName().Equals(constantName))
						    searchResult = FOUND;
					    else
						    constantIndex++;
				    }
    				
    				
				    if (searchResult == FOUND)
					    return constantIndex;
				    else
					    return NOT_FOUND;
    				
    				
			    } else 
				    return NOT_FOUND;
    			
		    }
    		
		    /**
		     * Gets constant associated with the expression.
		     * 
		     * @param      constantName        the constant name
		     * 
		     * @return     Constant if constant name was found,
		     *             otherwise return null.
		     * 
		     * @see        Constant
		     */
		    public Constant getConstant(String constantName) {
    			
			    int constantIndex = getConstantIndex(constantName);
    			
			    if (constantIndex == NOT_FOUND)
				    return null;
			    else
				    return constantsList[constantIndex];
    			
		    }
    		
		    /**
		     * Gets constant associated with the expression.
		     *  
		     * @param      constantIndex       the constant index
		     * 
		     * @return     Constant if the constantIndex is between
		     *             0 and the last available constant index
		     *             (getConstantsNumber() - 1),
		     *             otherwise it returns null.
		     * 
		     * @see        Constant
		     */
		    public Constant getConstant(int constantIndex) {
    			
			    if ( (constantIndex < 0) || (constantIndex >= constantsList.Count) )
				    return null;
			    else
				    return constantsList[constantIndex];
    			
		    }
    		
		    /**
		     * Gets number of constants associated with the expression.
		     *  
		     * @return     number of constants (int >= 0)
		     * 
		     * @see        Constant
		     */
		    public int getConstantsNumber() {
    	
			    return constantsList.Count;
    	
		    }
    			
    			
		    /**
		     * Removes first occurrences of the constants
		     * associated with the expression.
		     * 
		     * @param      constantsNames      the constants names (variadic parameters)
		     *                                 comma separated list
		     * 
		     * @see        Constant
		     */
		    public void removeConstants(params String[] constantsNames) {
    	
			    foreach (String constantName in constantsNames) {
    				
				    int constantIndex = getConstantIndex(constantName);
    				
				    if (constantIndex != NOT_FOUND) {
					    Constant c = constantsList[constantIndex];
					    c.removeRelatedExpression(this);
					    constantsList.RemoveAt( constantIndex );
				    }
    				
			    }
    			
			    setExpressionModifiedFlag();
    			
		    }
    	
		    /**
		     * Removes first occurrences of the constants
		     * associated with the expression
		     * 
		     * @param      constants           the constants (variadic parameters)
		     *                                 comma separated list
		     * 
		     * @see        Constant
		     */
		    public void removeConstants(params Constant[] constants) {
    	
			    foreach (Constant constant in constants) {
				    constantsList.Remove(constant);
				    constant.removeRelatedExpression(this);
				    setExpressionModifiedFlag();		
			    }
    			
		    }	
    		
		    /**
		     * Removes all constants
		     * associated with the expression
		     *
		     * @see        Constant
		     */
		    public void removeAllConstants() {
    	
			    foreach (Constant c in constantsList)
				    c.removeRelatedExpression(this);
    			
			    constantsList.Clear();
			    setExpressionModifiedFlag();		
    	
		    }	
    		
    	
		    /*=================================================
		     * 
		     * Functions handling API
		     * 
		     *================================================= 
		     */
    		
    		
    		
		    /**
		     * Adds functions (variadic parameters) to the expression definition.
		     * 
		     * @param      functions           the functions
		     *                                 (variadic parameters) comma separated list
		     * 
		     * @see        Function
		     */
		    public void addFunctions(params Function[] functions) {
    			
			    foreach (Function f in functions) {
				    functionsList.Add(f);
				    f.addRelatedExpression(this);			
			    }
    			
			    setExpressionModifiedFlag();		
    	
		    }
    	
		    /**
		     * Adds functions to the expression definition.
		     * 
		     * @param      functionsList       the functions list
		     * 
		     * @see        Function
		     */
		    public void addFunctions( List<Function> functionsList) {	
    	
			    this.functionsList.AddRange( functionsList );
    			
			    foreach (Function f in functionsList)
				    f.addRelatedExpression(this);
    			
			    setExpressionModifiedFlag();		
    	
		    }
    		
		    /**
		     * Enables to define the function (associated with 
		     * the expression) based on the function name,
		     * function expression string and arguments names (variadic parameters).
		     * 
		     * @param      functionName                  the function name
		     * @param      functionExpressionString      the expression string
		     * @param      argumentsNames                the function arguments names
		     *                                           (variadic parameters)
		     *                                           comma separated list
		     * 
		     * @see        Function
		     */
		    public void defineFunction(String functionName, String  functionExpressionString,
				    params String[] argumentsNames) {
    			
			    Function f = new Function(functionName, functionExpressionString, argumentsNames);
			    functionsList.Add(f);
			    f.addRelatedExpression(this);
    			
			    setExpressionModifiedFlag();
    			
		    }
    	
		    /**
		     * Gets index of function associated with the expression.
		     *  
		     * @param      functionName        the function name
		     * 
		     * @return     Function index if function name was found,
		     *             otherwise returns Function.NOT_FOUND
		     * 
		     * @see        Function
		     */
		    public int getFunctionIndex(String functionName) {
    	
			    int functionsNumber = functionsList.Count;
    			
			    if (functionsNumber > 0) {
    				
				    int functionIndex = 0;
				    int searchResult = NOT_FOUND; 
    				
				    while ((functionIndex < functionsNumber)
						    && (searchResult == NOT_FOUND)) {
    					
					    if (functionsList[functionIndex].getFunctionName().
							    Equals(functionName))
						    searchResult = FOUND;
					    else
						    functionIndex++;
    					
				    }
    				
    				
				    if (searchResult == FOUND)
					    return functionIndex;
				    else
					    return NOT_FOUND;
    				
    				
			    } else 
				    return NOT_FOUND;		
		    }
    		
		    /**
		     * Gets function associated with the expression.  
		     * 
		     * @param      functionName        the function name
		     * 
		     * @return     Function if function name was found,
		     *             otherwise returns null.
		     * 
		     * @see        Function
		     */
		    public Function getFunction(String functionName) {
    			
			    int functionIndex = getFunctionIndex(functionName);
    			
			    if (functionIndex == NOT_FOUND)
				    return null;
			    else
				    return functionsList[functionIndex];
    			
		    }	
    		
		    /**
		     * Gets function associated with the expression.
		     * 
		     * @param      functionIndex the function index
		     * 
		     * @return     Function if function index is between 0 and 
		     *             the last available function index (getFunctionsNumber()-1),
		     *             otherwise returns null.
		     * 
		     * @see        Function
		     */
		    public Function getFunction(int functionIndex) {
    			
			    if ( (functionIndex < 0) || (functionIndex >= functionsList.Count) )
				    return null;
			    else
				    return functionsList[functionIndex];
    			
		    }	
    	
		    /**
		     * Gets number of functions associated with the expression.
		     *  
		     * @return     number of functions (int >= 0)
		     * 
		     * @see        Function
		     */
		    public int getFunctionsNumber() {
    	
			    return functionsList.Count;
    	
		    }	
    		
		    /**
		     * Removes first occurrences of the functions
		     * associated with the expression.
		     * 
		     * @param      functionsNames      the functions names (variadic parameters)
		     *                                 comma separated list
		     * 
		     * @see        Function
		     */
		    public void removeFunctions(params String[] functionsNames) {
    	
    			
			    foreach (String functionName in functionsNames) {
    				
				    int functionIndex = getFunctionIndex(functionName);
    			
				    if (functionIndex != NOT_FOUND) {
					    Function f = functionsList[functionIndex];
					    f.removeRelatedExpression(this);
					    functionsList.Remove(f);
				    }
    			
			    }
    			
			    setExpressionModifiedFlag();
    			
		    }
    		
		    /**
		     * Removes first occurrences of the functions
		     * associated with the expression.
		     * 
		     * @param      functions           the functions (variadic parameters)
		     *                                 comma separated list.
		     * 
		     * @see        Function
		     */
		    public void removeFunctions(params Function[] functions) {
    	
			    foreach (Function function in functions) {
				    function.removeRelatedExpression(this);
				    functionsList.Remove(function);
			    }
			    setExpressionModifiedFlag();
    	
		    }
    	
		    /**
		     * Removes all functions
		     * associated with the expression.
		     * 
		     * @see        Function
		     */
		    public void removeAllFunctions() {
    	
			    foreach (Function f in functionsList)
				    f.removeRelatedExpression(this);
    			
			    functionsList.Clear();
			    setExpressionModifiedFlag();		
    	
		    }
    	
    	
    	
    	
	    /*=================================================
	     * 
	     * Common methods (supporting calculations)
	     * 
	     *================================================= 
	     */
    	
    	
	    /**
	     * Sets given token to the number type / value.
	     * Method should be called only by the SetDecreaseRemove like methods
	     * 
	     * @param      pos                 the position on which token
	     *                                 should be updated to the given number
	     * @param      number              the number
	     */
	    private void setToNumber(int pos, double number) {

		    Token token = tokensList[pos];
		    token.tokenValue = number;
		    token.tokenTypeId = ParserSymbol.NUMBER_TYPE_ID;
		    token.tokenId = ParserSymbol.NUMBER_ID;
		    token.keyWord = ParserSymbol.NUMBER_STR;

	    }

	    /**
	     * SetDecreaseRemove for 1 arg functions
	     * 
	     * SetDecreaseRemove like methods are called by the methods
	     * calculating values of the unary operation, binary relations
	     * and functions.
	     * 
	     * 3 things are done by this type of methods
	     * 1) Set token type to number type / value
	     * 2) Decrease level of the token
	     * 3) Remove no longer needed tokens
	     * 
	     * For example:
	     * 
	     * Expression string: 1+cos(0)
	     * will be tokened as follows:
	     * 
	     *   idx   :  0   1    2    3   4   5
	     *   token :  1   +   cos   (   0   ) 
	     *   level :  0   0    1    2   2   2 
	     *   
	     * Partitions with the highest level will be handled first.
	     * In the case presented above, it means, that the parenthesis will be removed
	     * 
	     *   idx   :  0   1    2    3
	     *   token :  1   +   cos   0 
	     *   level :  0   0    1    2 
	     * 
	     * Next step is to calculate cos(0) = 1
	     * 
	     * SetDecreaseRemove like methods
	     * 
	     * 1) Set cos token to 1 (pos=2, result=1):
 	     *   idx   :  0   1    2    3
	     *   token :  1   +    1    0 
	     *   level :  0   0    1    2 
	     *   
	     * 2) Decrease level (pos=2):
 	     *   idx   :  0   1    2    3
	     *   token :  1   +    1    0 
	     *   level :  0   0    0    2 
	     *   
	     * 3) Remove no longer needed tokens (pos+1=3):
 	     *   idx   :  0   1    2
	     *   token :  1   +    1 
	     *   level :  0   0    0 
	     * 
	     * @param      pos                 the position on which token
	     *                                 should be updated to the given number
	     * @param      result              the number
	     */	
	    private void f1SetDecreaseRemove(int pos, double result) {

		    setToNumber(pos, result);
		    tokensList[pos].tokenLevel--;
		    tokensList.RemoveAt(pos+1);

	    }

	    /**
	     * SetDecreaseRemove for 2-args functions
	     * 
	     * For detailed specification refer to the
	     *    f1SetDecreaseRemove()
	     *    
	     * @param      pos                 the position on which token
	     *                                 should be updated to the given number
	     * @param      result              the number
	     */	
	    private void f2SetDecreaseRemove(int pos, double result) {

		    setToNumber(pos, result);
		    tokensList[pos].tokenLevel--;
		    tokensList.RemoveAt(pos+2);
		    tokensList.RemoveAt(pos+1);

	    }

	    /**
	     * SetDecreaseRemove for 3-args functions
	     * 
	     * For detailed specification refer to the
	     *    f1SetDecreaseRemove()
	     *    
	     * @param      pos                 the position on which token
	     *                                 should be updated to the given number
	     * @param      result              the number
	     */		
	    private void f3SetDecreaseRemove(int pos, double result) {

		    setToNumber(pos, result);
		    tokensList[pos].tokenLevel--;
		    tokensList.RemoveAt(pos+3);
		    tokensList.RemoveAt(pos+2);
		    tokensList.RemoveAt(pos+1);

	    }
    	
	    /**
	     * SetDecreaseRemove for operators
	     * 
	     * For detailed specification refer to the
	     *    f1SetDecreaseRemove()
	     *    
	     * @param      pos                 the position on which token
	     *                                 should be updated to the given number
	     * @param      result              the number
	     */		
	    private void opSetDecreaseRemove(int pos, double result) {

		    setToNumber(pos, result);
		    tokensList.RemoveAt(pos+1);
		    tokensList.RemoveAt(pos-1);	

	    }

	    /**
	     * SetDecreaseRemove for calculus operators.
	     * 
	     * For detailed specification refer to the
	     *    f1SetDecreaseRemove()
	     *    
	     * @param      pos                 the position on which token
	     *                                 should be updated to the given number
	     * @param      result              the number
	     */
	    private void calcSetDecreaseRemove(int pos, double result) {

		    setToNumber(pos, result);
		    tokensList[pos].tokenLevel--;

		    /*
		     * left parethesis position
		     */
		    int lPos = pos+1;

		    /*
		     * Evaluate right parenthesis position
		     */
    		
		    int rPos = lPos+1;
		    while (	!(	( tokensList[rPos].tokenTypeId == ParserSymbol.TYPE_ID )
			    &&	( tokensList[rPos].tokenId == ParserSymbol.RIGHT_PARENTHESES_ID )
			    &&	( tokensList[rPos].tokenLevel ==  tokensList[lPos].tokenLevel)	)	)
			    rPos++;
    		
		    for (int p = rPos; p >= lPos; p--)
			    tokensList.RemoveAt(p);			
    		
	    }
    	
	    /**
	     * SetDecreaseRemove for special functions.
	     * 
	     * For detailed specification refer to the
	     *    f1SetDecreaseRemove()
	     *    
	     * @param      pos                 the position on which token
	     *                                 should be updated to the given number
	     * @param      result              the number
	     * @param      length              the special function range
	     */
	    private void specSetDecreaseRemove(int pos, double value, int length) {
    		
		    setToNumber(pos, value );
		    tokensList[pos].tokenLevel--;
    		
		    for (int p = pos + length; p > pos; p--)
			    tokensList.RemoveAt(p);		
    		
	    }
    	
	    /**
	     * If set remove method for the if function.
	     * 
	     * @param      pos                 the position
	     * @param      ifCondition         the result of if condition
	     */
	    private void ifSetRemove(int pos, double ifCondition) {

		    /*
		     * left parethesis position
		     */
		    int lPos = pos+1;		
		    int ifLevel =  tokensList[lPos].tokenLevel;
    		
    		

		    /*
		     * Evaluate 1 comma position on the same level
		     */		
		    int c1Pos = lPos+1;
		    while (	!(	( tokensList[c1Pos].tokenTypeId == ParserSymbol.TYPE_ID )
			    &&	( tokensList[c1Pos].tokenId == ParserSymbol.COMMA_ID )
			    &&	( tokensList[c1Pos].tokenLevel ==  ifLevel)	)	)
			    c1Pos++;

		    /*
		     * Evaluate 2 comma position on the same level
		     */
		    int c2Pos = c1Pos+1;
		    while (	!(	( tokensList[c2Pos].tokenTypeId == ParserSymbol.TYPE_ID )
			    &&	( tokensList[c2Pos].tokenId == ParserSymbol.COMMA_ID )
			    &&	( tokensList[c2Pos].tokenLevel ==  ifLevel)	)	)
			    c2Pos++;
    		
		    /*
		     * Evaluate right parenthesis position
		     */
		    int rPos = c2Pos+1;
		    while (	!(	( tokensList[rPos].tokenTypeId == ParserSymbol.TYPE_ID )
			    &&	( tokensList[rPos].tokenId == ParserSymbol.RIGHT_PARENTHESES_ID )
			    &&	( tokensList[rPos].tokenLevel ==  ifLevel)	)	)
			    rPos++;		
    		
    		
    		
		    int from;
		    int to;
    		
		    if ( !Double.IsNaN(ifCondition) ) {
    			
    					
			    if (ifCondition != 0) {

    				
				    setToNumber(c2Pos+1, Double.NaN);
				    tokensList[c2Pos+1].tokenLevel = ifLevel;
    				
				    from = c2Pos+2;
				    to = rPos-1;

    				
			    } else {
    				
				    setToNumber(c1Pos+1, Double.NaN);
				    tokensList[c1Pos+1].tokenLevel = ifLevel;
    				
				    from = c1Pos+2;
				    to = c2Pos-1;
    				
			    }
    				
			    if (from < to)
				    for (int p = to; p >= from; p--)
					    tokensList.RemoveAt(p);
    			
		    } else {
    			
			    setToNumber(c1Pos+1, Double.NaN);
			    setToNumber(c2Pos+1, Double.NaN);
			    tokensList[c1Pos+1].tokenLevel = ifLevel;
			    tokensList[c2Pos+1].tokenLevel = ifLevel;

			    from = c2Pos+2;
			    to = rPos-1;
			    if (from < to)
				    for (int p = to; p >= from; p--)
					    tokensList.RemoveAt(p);
    	
			    from = c1Pos+2;
			    to = c2Pos-1;			
			    if (from < to)
				    for (int p = to; p >= from; p--)
					    tokensList.RemoveAt(p);
    			
    			
		    }

		    setToNumber(lPos+1, ifCondition);
		    tokensList[lPos+1].tokenLevel = ifLevel;
    		
		    from = lPos+2;
		    to = c1Pos-1;
    		
		    if (from < to)
			    for (int p = to; p >= from; p--)
				    tokensList.RemoveAt(p);
    		
		    tokensList[pos].tokenId = Function3Arg.IF_ID;
    		
    		
	    }	
    	

	    /**
	     * Creates string tokens list from the subexpression.
	     * 
	     * @param      startPos            start position (index)
	     * @param      endPos              end position   (index)
	     * 
	     * @return     tokens list representing requested subexpression.
	     */
	    private List<Token> createInitialTokens(int startPos,
			    int endPos,
			    List<Token> tokensList) {
    		
		    List<Token> tokens = new List<Token>();
    		
		    Token t;
		    for (int p = startPos; p<= endPos; p++) {
			    t = tokensList[p].clone();				
			    tokens.Add(t);
		    }
    		
		    return tokens;

	    }

    	


 	    /**
	     * Return number of functions parameters.
	     * 
	     * @param      pos                 the function position
	     */	
	    private int getParametersNumber(int pos) {
    		
		    int lPpos = pos+1;
    		
		    if (lPpos == initialTokens.Count)
			    return -1;

		    if ( (initialTokens[lPpos].tokenTypeId == ParserSymbol.TYPE_ID) && (initialTokens[lPpos].tokenId == ParserSymbol.LEFT_PARENTHESES_ID) ) {

			    int tokenLevel = initialTokens[lPpos].tokenLevel;
			    /*
			     * Evaluate right parenthesis position
			     */
			    int endPos = lPpos+1;
			    while (	!(	( initialTokens[endPos].tokenTypeId == ParserSymbol.TYPE_ID )
				    &&	( initialTokens[endPos].tokenId == ParserSymbol.RIGHT_PARENTHESES_ID )
				    &&	( initialTokens[endPos].tokenLevel ==  tokenLevel)	)	)
				    endPos++;
    			
			    if (endPos == lPpos + 1)
				    return 0;
    			
			    /*
			     * Evaluate number of parameters by 
			     * counting number of ',' between parenthesis
			     */
			    int numberOfCommas = 0;
    					
			    for (int p = lPpos; p < endPos; p++) {
				    Token token = initialTokens[p];
    				
				    if ( (token.tokenTypeId == ParserSymbol.TYPE_ID) && (token.tokenId == ParserSymbol.COMMA_ID) && (token.tokenLevel == tokenLevel) )
					    numberOfCommas++;
			    }
    			
			    return numberOfCommas + 1;
    			
		    } else {
			    return -1;
		    }

	    }
    	
	    /**
	     * Returns list of the functions parameters.
	     * 
	     * @param      pos                 the function position
	     * @param      tokensList          the tokens list
	     * 
	     * @return     the list of function parameters
	     * 
	     * @see        FunctionParameter
	     */
	    private List<FunctionParameter> getFunctionParameters(int pos, List<Token> tokensList) {

		    List<FunctionParameter> functionParameters = new List<FunctionParameter>();
    		
		    int cPos = pos+2;
		    int tokenLevel = tokensList[pos+1].tokenLevel;
    		
		    int pPos = cPos;
    		
		    bool comma;
		    bool paren;		
		    bool end = false;
		    List<Token> paramTkones = new List<Token>();
		    String paramStr = "";
    		
		    do {
    			
			    Token t = tokensList[cPos];
			    comma = false;
			    paren = false;
    			
    			
			    if (t.tokenLevel == tokenLevel)
				    if (t.tokenTypeId == ParserSymbol.TYPE_ID) {
    					
					    if (t.tokenId == ParserSymbol.RIGHT_PARENTHESES_ID)
						    paren = true;
					    else
						    if (t.tokenId == ParserSymbol.COMMA_ID)
							    comma = true;
    						
				    }
    			
     
			    if ( (paren == true) || (comma == true) ) {
    				
    				
				    if (cPos > pos + 2) {
					    functionParameters.Add( new FunctionParameter(paramTkones, paramStr, pPos, cPos-1 ) );
					    paramTkones = new List<Token>();
					    paramStr = "";
					    pPos = cPos+1;
    					
				    }
    					
    					
    				
			    } else {
    				
				    paramTkones.Add(t);
				    paramStr = paramStr + t.tokenStr;
			    }
    				
			    if (paren)
				    end = true;
			    else
				    cPos++;
    			
    			
		    } while (!end);
    				
		    return functionParameters;

	    }
    	
    	
	    /**
	     * Gets / returns argument representing given argument name. If
	     * argument name exists on the list of known arguments
	     * the the initial status of the found argument is remembered, otherwise new
	     * argument will be created.
	     * 
	     * @param      argumentName        the argument name
	     * 
	     * @return     Argument parameter representing given argument name:
	     *             
	     * 
	     * @see        ArgumentParameter
	     * @see        Argument
	     */
	    private ArgumentParameter getParamArgument(String argumentName) {
    		
		    ArgumentParameter argParam = new ArgumentParameter();
    		
		    argParam.index = getArgumentIndex(argumentName);
		    argParam.argument = getArgument(argParam.index);
		    argParam.presence = FOUND;		
    		
		    if (argParam.argument == null) {
			    argParam.argument = new Argument(argumentName);
			    argumentsList.Add(argParam.argument);
			    argParam.index = argumentsList.Count-1;
			    argParam.presence = NOT_FOUND;
		    } else {
			    argParam.initialValue = argParam.argument.argumentValue;
			    argParam.initialType = argParam.argument.argumentType;
			    argParam.argument.argumentValue = argParam.argument.getArgumentValue();
			    argParam.argument.argumentType = Argument.FREE_ARGUMENT;
		    }
    		
		    return argParam;

	    }	
    	
	    /**
	     * Clears argument parameter.
	     * 
	     * @param      argParam            the argument parameter.
	     */
	    private void clearParamArgument(ArgumentParameter argParam) {
    				
		    if (argParam.presence == NOT_FOUND)
			    argumentsList.RemoveAt(argParam.index);
		    else {
			    argParam.argument.argumentValue = argParam.initialValue;
			    argParam.argument.argumentType = argParam.initialType;
		    }
    	
	    }	

    	
	    /*=================================================
	     * 
	     * Math implementations
	     * 
	     *================================================= 
	     */

    	
	    /**
	     * Arguments handling.
	     * 
	     * @param      pos                 the token position
	     */
	    private void ARGUMENT(int pos) {
    		
		    Argument argument = argumentsList[ tokensList[pos].tokenId ];
    		
    	
		    bool argumentVerboseMode = argument.getVerboseMode();
    			
		    if (verboseMode == true)
			    argument.setVerboseMode();
    			
		    setToNumber(pos,argument.getArgumentValue());
    			
		    if (argumentVerboseMode == false)
			    argument.setSilentMode();

	    }

    	
	    /**
	     * User functions handling.
	     * 
	     * @param      pos                 the token position
	     */
	    private void USER_FUNCTION(int pos) {
		    Function function;
    		
		    if (recursiveMode == true)
			    function = functionsList[ tokensList[pos].tokenId ].clone();
		    else
			    function = functionsList[ tokensList[pos].tokenId ];
    		
		    int argsNumber = function.getParametersNumber();
    		
		    for (int argIdx = 0; argIdx < argsNumber; argIdx++)
			    function.setArgumentValue(argIdx, tokensList[pos + argIdx + 1].tokenValue);
    		
		    bool functionVerboseMode = function.getVerboseMode();
    		
		    if (verboseMode == true)
			    function.setVerboseMode();
    		
		    double value;
		    try {	
			    value = function.calculate();
		    } catch(StackOverflowException soe){
			    value = Double.NaN;
		    }
    		
		    setToNumber(pos, value);
		    tokensList[pos].tokenLevel--;
    		
		    if (functionVerboseMode == false)
			    function.setSilentMode();		
    		
		    for (int argIdx = argsNumber; argIdx > 0 ; argIdx--)
			    tokensList.RemoveAt(pos+argIdx);
    		
    		
	    }	
    	
    	
	    /**
	     * User constants handling.
	     * 
	     * @param      pos                 the token position
	     */
	    private void USER_CONSTANT(int pos) {
    		
		    Constant constant = constantsList[ tokensList[pos].tokenId ];		
		    setToNumber(pos, constant.getConstantValue());
    		
	    }		
    	
	    /**
	     * Recursive arguments handling.
	     * 
	     * @param      pos                 the token position
	     */
	    private void RECURSIVE_ARGUMENT(int pos) {
    					
    	
		    double index = tokensList[pos+1].tokenValue;
		    RecursiveArgument argument = (RecursiveArgument)argumentsList[ tokensList[pos].tokenId ]; 
    		
		    bool argumentVerboseMode = argument.getVerboseMode();
    		
		    if (verboseMode == true)
			    argument.setVerboseMode();
    		
		    double result = argument.getArgumentValue(index);
    				
		    f1SetDecreaseRemove(pos, result);

		    if (argumentVerboseMode == false)
			    argument.setSilentMode();
    		
    		
	    }	
    	
	    /**
	     * COnstants handling handling.
	     * 
	     * @param      pos                 the token position
	     */	
	    private void CONSTANT(int pos) {
    		
    		
		    double constValue = Double.NaN;
    		
		    switch (tokensList[pos].tokenId) {
    		
		    case Const.PI_ID:
			    constValue = MathConstants.PI;
			    break;
    		
		    case Const.EULER_ID:
			    constValue = MathConstants.E;
			    break;
		    case Const.EULER_MASCHERONI_ID:
			    constValue = MathConstants.EULER_MASCHERONI;
			    break;
		    case Const.GOLDEN_RATIO_ID:
			    constValue = MathConstants.GOLDEN_RATIO;
			    break;
		    case Const.PLASTIC_ID:
			    constValue = MathConstants.PLASTIC;
			    break;
		    case Const.EMBREE_TREFETHEN_ID:
			    constValue = MathConstants.EMBREE_TREFETHEN;
			    break;
		    case Const.FEIGENBAUM_DELTA_ID:
			    constValue = MathConstants.FEIGENBAUM_DELTA;
			    break;
		    case Const.FEIGENBAUM_ALFA_ID:
			    constValue = MathConstants.FEIGENBAUM_ALFA;
			    break;
		    case Const.TWIN_PRIME_ID:
			    constValue = MathConstants.TWIN_PRIME;
			    break;
		    case Const.MEISSEL_MERTEENS_ID:
			    constValue = MathConstants.MEISSEL_MERTEENS;
			    break;
		    case Const.BRAUN_TWIN_PRIME_ID:
			    constValue = MathConstants.BRAUN_TWIN_PRIME;
			    break;
		    case Const.BRAUN_PRIME_QUADR_ID:
			    constValue = MathConstants.BRAUN_PRIME_QUADR;
			    break;
		    case Const.BRUIJN_NEWMAN_ID:
			    constValue = MathConstants.BRUIJN_NEWMAN;
			    break;
		    case Const.CATALAN_ID:
			    constValue = MathConstants.CATALAN;
			    break;
		    case Const.LANDAU_RAMANUJAN_ID:
			    constValue = MathConstants.LANDAU_RAMANUJAN;
			    break;
		    case Const.VISWANATH_ID:
			    constValue = MathConstants.VISWANATH;
			    break;
		    case Const.LEGENDRE_ID:
			    constValue = MathConstants.LEGENDRE;
			    break;
		    case Const.RAMANUJAN_SOLDNER_ID:
			    constValue = MathConstants.RAMANUJAN_SOLDNER;
			    break;
		    case Const.ERDOS_BORWEIN_ID:
			    constValue = MathConstants.ERDOS_BORWEIN;
			    break;
		    case Const.BERNSTEIN_ID:
			    constValue = MathConstants.BERNSTEIN;
			    break;
		    case Const.GAUSS_KUZMIN_WIRSING_ID:
			    constValue = MathConstants.GAUSS_KUZMIN_WIRSING;
			    break;
		    case Const.HAFNER_SARNAK_MCCURLEY_ID:
			    constValue = MathConstants.HAFNER_SARNAK_MCCURLEY;
			    break;
		    case Const.GOLOMB_DICKMAN_ID:
			    constValue = MathConstants.GOLOMB_DICKMAN;
			    break;
		    case Const.CAHEN_ID:
			    constValue = MathConstants.CAHEN;
			    break;
		    case Const.LAPLACE_LIMIT_ID:
			    constValue = MathConstants.LAPLACE_LIMIT;
			    break;
		    case Const.ALLADI_GRINSTEAD_ID:
			    constValue = MathConstants.ALLADI_GRINSTEAD;
			    break;
		    case Const.LENGYEL_ID:
			    constValue = MathConstants.LENGYEL;
			    break;
		    case Const.LEVY_ID:
			    constValue = MathConstants.LEVY;
			    break;
		    case Const.APERY_ID:
			    constValue = MathConstants.APERY;
			    break;
		    case Const.MILLS_ID:
			    constValue = MathConstants.MILLS;
			    break;
		    case Const.BACKHOUSE_ID:
			    constValue = MathConstants.BACKHOUSE;
			    break;
		    case Const.PORTER_ID:
			    constValue = MathConstants.PORTER;
			    break;
		    case Const.LIEB_QUARE_ICE_ID:
			    constValue = MathConstants.LIEB_QUARE_ICE;
			    break;
		    case Const.NIVEN_ID:
			    constValue = MathConstants.NIVEN;
			    break;
		    case Const.SIERPINSKI_ID:
			    constValue = MathConstants.SIERPINSKI;
			    break;
		    case Const.KHINCHIN_ID:
			    constValue = MathConstants.KHINCHIN;
			    break;
		    case Const.FRANSEN_ROBINSON_ID:
			    constValue = MathConstants.FRANSEN_ROBINSON;
			    break;
		    case Const.LANDAU_ID:
			    constValue = MathConstants.LANDAU;
			    break;
		    case Const.PARABOLIC_ID:
			    constValue = MathConstants.PARABOLIC;
			    break;
		    case Const.OMEGA_ID:
			    constValue = MathConstants.OMEGA;
			    break;
		    case Const.MRB_ID:
			    constValue = MathConstants.MRB;
			    break;
    		
		    }
    		
		    setToNumber(pos, constValue);
    		
	    }
    	

	    /**
	     * Gets token value
	     * @param      tokenIndex          the token index
	     * 
	     * @return     the token value
	     */
	    private double getTokenValue(int tokenIndex) {
		    return tokensList[tokenIndex].tokenValue;
	    }
    	
    	
	    /**
	     * Power handling.
	     * 
	     * @param      pos                 the token position
	     */
	    private void POWER(int pos) {

		    double a = getTokenValue(pos-1); 
		    double b = getTokenValue(pos+1);
		    opSetDecreaseRemove(pos, MathFunctions.power(a, b) );

	    }

	    /**
	     * Modulo handling.
	     * 
	     * @param      pos                 the token position
	     */
	    private void MODULO(int pos) {

		    double a = getTokenValue(pos-1); 
		    double b = getTokenValue(pos+1);
		    opSetDecreaseRemove(pos, MathFunctions.mod(a, b) );

	    }	
    	
	    /**
	     * Division handling.
	     * 
	     * @param      pos                 the token position
	     */
	    private void DIVIDE(int pos) {
    		
		    double a = getTokenValue(pos-1); 
		    double b = getTokenValue(pos+1);
    	
		    opSetDecreaseRemove(pos, MathFunctions.div(a, b) );

	    }

	    /**
	     * Multiplication handling.
	     * 
	     * @param      pos                 the token position
	     */
	    private void MULTIPLY(int pos) {
    		
		    double a = getTokenValue(pos-1); 
		    double b = getTokenValue(pos+1);
		    opSetDecreaseRemove(pos, a * b );

	    }	
    	
	    /**
	     * Addition handling.
	     * 
	     * @param      pos                 the token position
	     */
	    private void PLUS(int pos) {
    		
		    Token b = tokensList[pos+1];
    		
		    if (pos>0) {
			    Token a = tokensList[pos-1];
    			
			    if ( (a.tokenTypeId == ParserSymbol.NUMBER_TYPE_ID) && (b.tokenTypeId == ParserSymbol.NUMBER_TYPE_ID))
				    opSetDecreaseRemove(pos, a.tokenValue + b.tokenValue );
			    else if (b.tokenTypeId == ParserSymbol.NUMBER_TYPE_ID) {
				    setToNumber(pos,b.tokenValue);
				    tokensList.RemoveAt(pos+1);
			    }
		    }
		    else
			    if (b.tokenTypeId == ParserSymbol.NUMBER_TYPE_ID) {
				    setToNumber(pos,b.tokenValue);
				    tokensList.RemoveAt(pos+1);				
			    }


	    }	

	    /**
	     * Subtraction handling
	     * 
	     * @param      pos                 the token position
	     */
	    private void MINUS(int pos) {
    			
		    Token b = tokensList[pos+1];
    		
		    if (pos>0) {
			    Token a = tokensList[pos-1];
    			
			    if ( (a.tokenTypeId == ParserSymbol.NUMBER_TYPE_ID) && (b.tokenTypeId == ParserSymbol.NUMBER_TYPE_ID))
				    opSetDecreaseRemove(pos, a.tokenValue - b.tokenValue );
			    else if (b.tokenTypeId == ParserSymbol.NUMBER_TYPE_ID) {
				    setToNumber(pos,-b.tokenValue);
				    tokensList.RemoveAt(pos+1);
			    }
		    }
		    else
			    if (b.tokenTypeId == ParserSymbol.NUMBER_TYPE_ID) {
				    setToNumber(pos,-b.tokenValue);
				    tokensList.RemoveAt(pos+1);				
			    }

	    }	

	    /**
	     * Logical AND
	     * 
	     * @param      pos                 the token position
	     */
	    private void AND(int pos) {

		    double a = getTokenValue(pos-1); 
		    double b = getTokenValue(pos+1);
    				
		    opSetDecreaseRemove(pos, BooleanAlgebra.and(a, b) );

	    }

	    /**
	     * Logical OR
	     * 
	     * @param      pos                 the token position
	     */
	    private void OR(int pos) {

		    double a = getTokenValue(pos-1); 
		    double b = getTokenValue(pos+1);
    		
		    opSetDecreaseRemove(pos, BooleanAlgebra.or(a, b) );

	    }	
    	
	    /**
	     * Logical NAND
	     * 
	     * @param      pos                 the token position
	     */
	    private void NAND(int pos) {

		    double a = getTokenValue(pos-1); 
		    double b = getTokenValue(pos+1);
    		
		    opSetDecreaseRemove(pos, BooleanAlgebra.nand(a, b) );

	    }	

	    /**
	     * Logical NOR
	     * 
	     * @param      pos                 the token position
	     */
	    private void NOR(int pos) {

		    double a = getTokenValue(pos-1); 
		    double b = getTokenValue(pos+1);
    		
		    opSetDecreaseRemove(pos, BooleanAlgebra.nor(a, b) );

	    }	

	    /**
	     * Logical XOR
	     * 
	     * 
	     * @param      pos                 the token position
	     */
	    private void XOR(int pos) {

		    double a = getTokenValue(pos-1); 
		    double b = getTokenValue(pos+1);
    		
		    opSetDecreaseRemove(pos, BooleanAlgebra.xor(a, b) );

	    }	

	    /**
	     * Logical IMP
	     *
	     * 
	     * @param      pos                 the token position
	     */
	    private void IMP(int pos) {

		    double a = getTokenValue(pos-1); 
		    double b = getTokenValue(pos+1);
    		
		    opSetDecreaseRemove(pos, BooleanAlgebra.imp(a, b) );

	    }	

	    /**
	     * Logical CIMP
	     * 
	     * @param      pos                 the token position
	     */	
	    private void CIMP(int pos) {

		    double a = getTokenValue(pos-1); 
		    double b = getTokenValue(pos+1);
    		
		    opSetDecreaseRemove(pos, BooleanAlgebra.cimp(a, b) );

	    }	
    	
	    /**
	     * Logical NIMP
	     * 
	     * @param      pos                 the token position
	     */
	    private void NIMP(int pos) {

		    double a = getTokenValue(pos-1); 
		    double b = getTokenValue(pos+1);
    		
		    opSetDecreaseRemove(pos, BooleanAlgebra.nimp(a, b) );

	    }	

	    /**
	     * Logical CNIMP
	     * 
	     * @param      pos                 the token position
	     */
	    private void CNIMP(int pos) {

		    double a = getTokenValue(pos-1); 
		    double b = getTokenValue(pos+1);
    		
		    opSetDecreaseRemove(pos, BooleanAlgebra.cnimp(a, b) );

	    }	
    	
	    /**
	     * Logical EQV
	     * 
	     * @param      pos                 the token position
	     */
	    private void EQV(int pos) {

		    double a = getTokenValue(pos-1); 
		    double b = getTokenValue(pos+1);
    		
		    opSetDecreaseRemove(pos, BooleanAlgebra.eqv(a, b) );


	    }	
    	
	    /**
	     * Logical negation
	     * 
	     * @param      pos                 the token position
	     */
	    private void NEG(int pos) {

		    double a = getTokenValue(pos+1);	
		    setToNumber(pos, BooleanAlgebra.not(a) );
		    tokensList.RemoveAt(pos+1);
	    }
    	
    	
	    /**
	     * Equality relation.
	     * 
	     * @param      pos                 the token position
	     */
	    private void EQ(int pos) {

		    double a = getTokenValue(pos-1); 
		    double b = getTokenValue(pos+1);
    			
		    opSetDecreaseRemove(pos, BinaryRelations.eq(a, b) );

	    }

	    /**
	     * Not equals.
	     * 
	     * @param      pos                 the token position
	     */
	    private void NEQ(int pos) {

		    double a = getTokenValue(pos-1); 
		    double b = getTokenValue(pos+1);	
    		
		    opSetDecreaseRemove(pos, BinaryRelations.neq(a, b) );

	    }

	    /**
	     * Lower than.
	     * 
	     * @param      pos                 the token position
	     */
	    private void LT(int pos) {

		    double a = getTokenValue(pos-1); 
		    double b = getTokenValue(pos+1);
    		
		    opSetDecreaseRemove(pos, BinaryRelations.lt(a, b) );

	    }
    	
	    /**
	     * Greater than.
	     * 
	     * @param      pos                 the token position
	     */
	    private void GT(int pos) {

		    double a = getTokenValue(pos-1); 
		    double b = getTokenValue(pos+1);
    		
		    opSetDecreaseRemove(pos, BinaryRelations.gt(a, b) );

	    }
    	
	    /**
	     * Lower or equal.
	     * 
	     * @param      pos                 the token position
	     */
	    private void LEQ(int pos) {

		    double a = getTokenValue(pos-1); 
		    double b = getTokenValue(pos+1);
    		
		    opSetDecreaseRemove(pos, BinaryRelations.leq(a, b) );

	    }
    	
	    /**
	     * Greater or equal
	     * 
	     * @param      pos                 the token position
	     */
	    private void GEQ(int pos) {

		    double a = getTokenValue(pos-1); 
		    double b = getTokenValue(pos+1);
    		
		    opSetDecreaseRemove(pos, BinaryRelations.geq(a, b) );

	    }
    	
    	
	    /**
	     * Sine function
	     * 
	     * @param      pos                 the token position
	     */
	    private void SIN(int pos) {

		    double a = getTokenValue(pos+1);
		    f1SetDecreaseRemove(pos, MathFunctions.sin(a) );

	    }
    	
	    /**
	     * Cosine / Trigonometric functions
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */					
	    private void COS(int pos) {

		    double a = getTokenValue(pos+1);
		    f1SetDecreaseRemove(pos, MathFunctions.cos(a) );

	    }

	    /**
	     * Tangent / Trigonometric functions
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */						
	    private void TAN(int pos) {

		    double a = getTokenValue(pos+1);
		    f1SetDecreaseRemove(pos, MathFunctions.tan(a) );

	    }

	    /**
	     * Cotangent / Trigonometric functions
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */						
	    private void CTAN(int pos) {

		    double a = getTokenValue(pos+1);
    		
		    f1SetDecreaseRemove(pos, MathFunctions.ctan(a) );

	    }	

	    /**
	     * Secant / Trigonometric functions
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */							
	    private void SEC(int pos) {

		    double a = getTokenValue(pos+1);
    		
		    f1SetDecreaseRemove(pos, MathFunctions.sec(a) );

	    }	

	    /**
	     * Cosecant / Trigonometric functions
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */							
	    private void COSEC(int pos) {

		    double a = getTokenValue(pos+1);	
		    f1SetDecreaseRemove(pos, MathFunctions.cosec(a) );

	    }
    	
	    /**
	     * Arcus sine / Inverse trigonometric functions
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */							
	    private void ASIN(int pos) {

		    double a = getTokenValue(pos+1);
		    f1SetDecreaseRemove(pos, MathFunctions.asin(a) );

	    }
    	
	    /**
	     * Arcus cosine / Inverse trigonometric functions
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */							
	    private void ACOS(int pos) {

		    double a = getTokenValue(pos+1);
		    f1SetDecreaseRemove(pos, MathFunctions.acos(a) );

	    }

	    /**
	     * Arcus tangent / Inverse trigonometric functions
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */								
	    private void ATAN(int pos) {

		    double a = getTokenValue(pos+1);
		    f1SetDecreaseRemove(pos, MathFunctions.atan(a) );

	    }
    	
	    /**
	     * Arcus cotangent / Inverse trigonometric functions
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */							
	    private void ACTAN(int pos) {

		    double a = getTokenValue(pos+1);
		    f1SetDecreaseRemove(pos, MathFunctions.actan(a) );

	    }

	    /**
	     * Natural logarithm (base e)
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */								
	    private void LN(int pos) {

		    double a = getTokenValue(pos+1);
		    f1SetDecreaseRemove(pos, MathFunctions.ln(a) );

	    }
    	
	    /**
	     * Logarithm - base 2
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */								
	    private void LOG2(int pos) {

		    double a = getTokenValue(pos+1);
		    f1SetDecreaseRemove(pos, MathFunctions.log2(a) );

	    }
    	
	    /**
	     * Logarithm - base 10
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */								
	    private void LOG10(int pos) {

		    double a = getTokenValue(pos+1);
		    f1SetDecreaseRemove(pos, MathFunctions.log10(a) );

	    }
    	
	    /**
	     * Converts degrees to radius
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */								
	    private void RAD(int pos) {

		    double a = getTokenValue(pos+1);
		    f1SetDecreaseRemove(pos, MathFunctions.rad(a) );

	    }
    	
	    /**
	     * Exponential function
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */								
	    private void EXP(int pos) {

		    double a = getTokenValue(pos+1);
		    f1SetDecreaseRemove(pos, MathFunctions.exp(a) );

	    }

	    /**
	     * Square root
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */								
	    private void SQRT(int pos) {

		    double a = getTokenValue(pos+1);
		    f1SetDecreaseRemove(pos, MathFunctions.sqrt(a) );

	    }
    	
	    /**
	     * Hyperbolic sine
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */								
	    private void SINH(int pos) {

		    double a = getTokenValue(pos+1);
		    f1SetDecreaseRemove(pos, MathFunctions.sinh(a) );

	    }

	    /**
	     * Hyperbolic cosine
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */								
	    private void COSH(int pos) {

		    double a = getTokenValue(pos+1);
		    f1SetDecreaseRemove(pos, MathFunctions.cosh(a) );

	    }
    	
	    /**
	     * Hyperbolic tangent
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */								
	    private void TANH(int pos) {

		    double a = getTokenValue(pos+1);
		    f1SetDecreaseRemove(pos, MathFunctions.tanh(a) );

	    }

	    /**
	     * Hyperbolic cotangent
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */								
	    private void COTH(int pos) {

		    double a = getTokenValue(pos+1);		
		    f1SetDecreaseRemove(pos, MathFunctions.coth(a) );

	    }
    	
	    /**
	     * Hyperbolic secant
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */								
	    private void SECH(int pos) {

		    double a = getTokenValue(pos+1);
		    f1SetDecreaseRemove(pos, MathFunctions.sech(a) );

	    }

	    /**
	     * Hyperbolic cosecant
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */								
	    private void CSCH(int pos) {

		    double a = getTokenValue(pos+1);		
		    f1SetDecreaseRemove(pos, MathFunctions.csch(a) );

	    }

	    /**
	     * Converts radius to degrees
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */								
	    private void DEG(int pos) {

		    double a = getTokenValue(pos+1);
		    f1SetDecreaseRemove(pos, MathFunctions.deg(a) );

	    }

	    /**
	     * Absolut value
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */								
	    private void ABS(int pos) {

		    double a = getTokenValue(pos+1);
		    f1SetDecreaseRemove(pos, MathFunctions.abs(a) );

	    }

	    /**
	     * Signum function
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */								
	    private void SGN(int pos) {

		    double a = getTokenValue(pos+1);
		    f1SetDecreaseRemove(pos, MathFunctions.sgn(a) );

	    }

	    /**
	     * Floor function
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */								
	    private void FLOOR(int pos) {

		    double a = getTokenValue(pos+1);
		    f1SetDecreaseRemove(pos, MathFunctions.floor(a) );

	    }
    	
	    /**
	     * Ceil function
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */								
	    private void CEIL(int pos) {

		    double a = getTokenValue(pos+1);
		    f1SetDecreaseRemove(pos, MathFunctions.ceil(a) );
    	
	    }
    	
	    /**
	     * Arcus hyperbolic sine
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */								
	    private void ARSINH(int pos) {

		    double a = getTokenValue(pos+1);		
		    f1SetDecreaseRemove(pos, MathFunctions.arsinh(a) );

	    }
    	
	    /**
	     * Arcus hyperbolic cosine
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */								
	    private void ARCOSH(int pos) {

		    double a = getTokenValue(pos+1);
		    f1SetDecreaseRemove(pos, MathFunctions.arcosh(a) );

	    }
    	
	    /**
	     * Arcus hyperbolic tangent
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */								
	    private void ARTANH(int pos) {

		    double a = getTokenValue(pos+1);		
		    f1SetDecreaseRemove(pos, MathFunctions.artanh(a) );

	    }
    	
	    /**
	     * Arcus hyperbolic cotangent
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */								
	    private void ARCOTH(int pos) {

		    double a = getTokenValue(pos+1);		
		    f1SetDecreaseRemove(pos, MathFunctions.arcoth(a) );

	    }
    	
	    /**
	     * Arcus hyperbolic secant
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */								
	    private void ARSECH(int pos) {

		    double a = getTokenValue(pos+1);
		    f1SetDecreaseRemove(pos, MathFunctions.arsech(a) );

	    }
    	
	    /**
	     * Arcus hyperbolic cosecant
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */								
	    private void ARCSCH(int pos) {

		    double a = getTokenValue(pos+1);		
		    f1SetDecreaseRemove(pos, MathFunctions.arcsch(a) );

	    }
    	
    	
	    /**
	     * SA / sinc normalized
	     * 
	     * @param      pos                 the token position
	     */								
	    private void SA(int pos) {

		    double a = getTokenValue(pos+1);
		    f1SetDecreaseRemove(pos, MathFunctions.sa(a) );

	    }	

	    /**
	     * Sinc unnormalized
	     * 
	     * @param      pos                 the token position
	     */								
	    private void SINC(int pos) {

		    double a = getTokenValue(pos+1);
		    f1SetDecreaseRemove(pos, MathFunctions.sinc(a) );

	    }	
    	
	    /**
	     * Bell numbers
	     * 
	     * @param      pos                 the token position
	     */
	    private void BELL_NUMBER(int pos) {

		    double n = getTokenValue(pos+1);
    			
		    f1SetDecreaseRemove(pos, MathFunctions.bellNumber(n) );
    	
	    }
    	
    	
	    /**
	     * Lucas numbers
	     * 
	     * @param      pos                 the token position
	     */
	    private void LUCAS_NUMBER(int pos) {

		    double n = getTokenValue(pos+1);
    			
		    f1SetDecreaseRemove(pos, MathFunctions.lucasNumber(n) );
    	
	    }
    	
	    /**
	     * Fibonacci numbers
	     * 
	     * @param      pos                 the token position
	     */
	    private void FIBONACCI_NUMBER(int pos) {

		    double n = getTokenValue(pos+1);
    			
		    f1SetDecreaseRemove(pos, MathFunctions.fibonacciNumber(n) );
    	
	    }		
    	
	    /**
	     * Harmonic numbers
	     * 
	     * @param      pos                 the token position
	     */
	    private void HARMONIC_NUMBER(int pos) {

		    double n = getTokenValue(pos+1);
    			
		    f1SetDecreaseRemove(pos, MathFunctions.harmonicNumber(n) );
    	
	    }		
    		
    	
	    /**
	     * Factorilal function
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */									
	    private void FACT(int pos) {

		    double a = getTokenValue(pos-1);
		    setToNumber(pos, MathFunctions.factorial(a));
		    tokensList.RemoveAt(pos-1);

	    }	
    	

	    /**
	     * Negation
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */									
	    private void NOT(int pos) {

		    double a = getTokenValue(pos+1);	
		    f1SetDecreaseRemove(pos, BooleanAlgebra.not(a) );

	    }
    	
    	
	    /**
	     * Logarithm
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */				
	    private void LOG(int pos) {

		    double b = getTokenValue(pos+1); 
		    double a = getTokenValue(pos+2);
		    f2SetDecreaseRemove(pos, MathFunctions.log(a, b) );	

	    }

	    /**
	     * Creates ArraList<Double> containing function parameters
	     * 
	     * @param      pos                 the function position
	     * 
	     * @return     List of function parameters.
	     */
	    private List<Double> getNumbers(int pos) {

		    List<Double> numbers = new List<Double>();
    		
		    int pn = pos;
		    int lastIndex = tokensList.Count - 1;
		    bool isNumber; 
		    bool end = false;
    		
		    do {
			    pn++;
    			
			    Token t = tokensList[pn];
			    isNumber = false;
    			
			    if ( (t.tokenTypeId == ParserSymbol.NUMBER_TYPE_ID) && (t.tokenId == ParserSymbol.NUMBER_ID) ) {	
				    isNumber = true;
				    numbers.Add(t.tokenValue);
			    }
    			
			    if ( (pn == lastIndex) || (!isNumber) )
				    end = true;
    			
    			
		    } while ( end == false );

		    return numbers;
    		
	    }
    	
    	
	    /**
	     * Modulo
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */		
	    private void MOD(int pos) {

		    double a = getTokenValue(pos+1); 
		    double b = getTokenValue(pos+2);
		    f2SetDecreaseRemove(pos, MathFunctions.mod(a, b) );	

	    }
    	
    	
	    /**
	     * Binomial Coefficient
	     * 
	     * @param      pos                 the token position
	     */
	    private void BINOM_COEFF(int pos) {

		    double a = getTokenValue(pos+1); 
		    double b = getTokenValue(pos+2);
    				
		    f2SetDecreaseRemove(pos, MathFunctions.binomCoeff(a,b) );	

	    }
    		
	    /**
	     * Bernoulli Number
	     * 
	     * @param      pos                 the token position
	     */
	    private void BERNOULLI_NUMBER(int pos) {

		    double m = getTokenValue(pos+1); 
		    double n = getTokenValue(pos+2);
    				
		    f2SetDecreaseRemove(pos, MathFunctions.bernoulliNumber(m, n) );	

	    }


	    /**
	     * Stirling number of the first kind
	     * 
	     * @param      pos                 the token position
	     */
	    private void STIRLING1_NUMBER(int pos) {

		    double n = getTokenValue(pos+1); 
		    double k = getTokenValue(pos+2);
    				
		    f2SetDecreaseRemove(pos, MathFunctions.Srirling1Number(n, k) );	

	    }
    	
    	
	    /**
	     * Stirling number of the second kind.
	     * 
	     * @param      pos                 the token position
	     */
	    private void STIRLING2_NUMBER(int pos) {

		    double n = getTokenValue(pos+1); 
		    double k = getTokenValue(pos+2);
    				
		    f2SetDecreaseRemove(pos, MathFunctions.Srirling2Number(n, k) );	

	    }
    	
	    /**
	     * Worpitzky number.
	     * 
	     * @param      pos                 the token position
	     */
	    private void WORPITZKY_NUMBER(int pos) {

		    double n = getTokenValue(pos+1); 
		    double k = getTokenValue(pos+2);
    				
		    f2SetDecreaseRemove(pos, MathFunctions.worpitzkyNumber(n, k) );	

	    }
    			
	    /**
	     * Euler number
	     * 
	     * @param      pos                 the token position
	     */
	    private void EULER_NUMBER(int pos) {

		    double n = getTokenValue(pos+1); 
		    double k = getTokenValue(pos+2);
    		
		    f2SetDecreaseRemove(pos, MathFunctions.eulerNumber(n, k) );

	    }		
    	
	    /**
	     * Kronecker delta
	     * 
	     * @param      pos                 the token position
	     */
	    private void KRONECKER_DELTA(int pos) {

		    double i = getTokenValue(pos+1); 
		    double j = getTokenValue(pos+2);
    				
		    f2SetDecreaseRemove(pos, MathFunctions.kroneckerDelta(i, j) );	

	    }
    	
    	
	    /**
	     * Euler polynomial
	     * 
	     * @param      pos                 the token position
	     */
	    private void EULER_POLYNOMIAL(int pos) {

		    double m = getTokenValue(pos+1); 
		    double x = getTokenValue(pos+2);
    				
		    f2SetDecreaseRemove(pos, MathFunctions.eulerPolynomial(m, x) );	

	    }
    	
	    /**
	     * Harmonic numbers
	     * 
	     * @param      pos                 the token position
	     */
	    private void HARMONIC2_NUMBER(int pos) {

		    double x = getTokenValue(pos+1);
		    double n = getTokenValue(pos+2);
    			
		    f2SetDecreaseRemove(pos, MathFunctions.harmonicNumber(x, n) );
    	
	    }			
    	
	    /**
	     * IF function
	     * 
	     * @param      pos                 the token position
	     */
	    private void IF_CONDITION(int pos) {
    		
		    /*
		     * Get condition string
		     * 1st parameter
		     */
    		
		    List<FunctionParameter> ifParams = getFunctionParameters(pos, tokensList);
    		
		    FunctionParameter ifParam = ifParams[0];
		    Expression ifExp = new Expression(ifParam.paramStr, ifParam.tokens, argumentsList, functionsList, constantsList);
    		
		    if (verboseMode == true)
			    ifExp.setVerboseMode();
    		
		    ifSetRemove(pos, ifExp.calculate());
    		
	    }
    	
	    /**
	     * IFF function
	     * 
	     * @param      pos                 the token position
	     */
	    private void IFF(int pos) {
    		
		    /*
		     * Get condition string
		     * 1st parameter
		     */
    		
		    List<FunctionParameter> iffParams = getFunctionParameters(pos, tokensList);
    		
		    FunctionParameter iffParam = iffParams[0];
		    int parametersNumber = iffParams.Count;
    		
		    int trueParamNumber;
		    int paramNumber;
    		
    		
		    paramNumber = 1;
    		
		    Expression iffExp;
    		
		    double iffValue = 0;
		    bool iffCon = true;
    		
		    do {
    		
			    iffExp = new Expression(iffParam.paramStr, iffParam.tokens, argumentsList, functionsList, constantsList);
    			
			    if (verboseMode == true)
				    iffExp.setVerboseMode();
    			
    			
			    iffCon = true;
			    iffValue = iffExp.calculate();
    			
			    if ( (iffValue == 0) || (Double.IsNaN(iffValue)) ) {
					    paramNumber += 2;
					    iffCon = false;
    					
					    if (paramNumber < parametersNumber)
						    iffParam = iffParams[paramNumber-1];
			    }
    		
		    } while ( (!iffCon) && (paramNumber < parametersNumber) );

		    int from;
		    int to;
		    int p;
    		
		    if (iffCon) {
    			
			    trueParamNumber = paramNumber+1;

			    from = pos+1;
			    to = iffParams[parametersNumber-1].toIndex+1;
			    tokensList[from].tokenLevel--;
			    tokensList[to].tokenLevel--;
    			
			    if (trueParamNumber < parametersNumber) {
				    to = iffParams[parametersNumber-1].toIndex;
				    from = iffParams[trueParamNumber].fromIndex-1;
				    for (p = to; p >= from; p--)
					    tokensList.RemoveAt(p);
			    }

			    from = iffParams[trueParamNumber-1].fromIndex;
			    to = iffParams[trueParamNumber-1].toIndex;
			    for (p = from; p <= to; p++)
				    tokensList[p].tokenLevel--;
    			
			    to = from-1;
			    from = pos;
			    for (p = to; p >= from; p--)
				    if (p != pos + 1)
				    tokensList.RemoveAt(p);
    			
    			
		    } else {
    			
			    to = iffParams[parametersNumber-1].toIndex+1;
			    from = pos+1;
			    for (p = to; p >= from; p--)
				    tokensList.RemoveAt(p);
    			
			    setToNumber(pos, Double.NaN);
			    tokensList[pos].tokenLevel--;
    			
		    }
    		
    			
    		
	    }	
    	
	    /**
	     * IF
	     * Sets tokens to number token
	     * 
	     * @param pos token index (position)
	     */		
	    private void IF(int pos) {

		    double ifCondition = tokensList[pos+1].tokenValue; 
		    double ifTrue = tokensList[pos+2].tokenValue;
		    double ifFalse = tokensList[pos+3].tokenValue;

		    double result = ifFalse;
    		
		    if (ifCondition != 0)
			    result = ifTrue;
    		
		    if (ifCondition == Double.NaN)
			    result = Double.NaN;
    		
		    f3SetDecreaseRemove(pos, result );
    		
	    }
    	

    	
	    /**
	     * Characteristic function (a,b)
	     * 
	     * @param      pos                 the token position
	     */
	    private void CHI_ab(int pos) {

		    double x = getTokenValue(pos+1); 
		    double a = getTokenValue(pos+2);
		    double b = getTokenValue(pos+3);
    		
		    f3SetDecreaseRemove(pos, MathFunctions.chi(x, a, b) );
    		
	    }	

	    /**
	     * Characteristic function [a,b]
	     * 
	     * @param      pos                 the token position
	     */
	    private void CHI_AB(int pos) {

		    double x = getTokenValue(pos+1); 
		    double a = getTokenValue(pos+2);
		    double b = getTokenValue(pos+3);
    		
		    f3SetDecreaseRemove(pos, MathFunctions.CHi(x, a, b) );
    		
	    }	

    	
	    /**
	     * Characteristic function [a,b)
	     * 
	     * @param      pos                 the token position
	     */
	    private void CHI_Ab(int pos) {

		    double x = getTokenValue(pos+1); 
		    double a = getTokenValue(pos+2);
		    double b = getTokenValue(pos+3);
    		
		    f3SetDecreaseRemove(pos, MathFunctions.Chi(x, a, b) );
    		
	    }	

    	
	    /**
	     * Characteristic function (a,b]
	     * 
	     * @param      pos                 the token position
	     */
	    private void CHI_aB(int pos) {

		    double x = getTokenValue(pos+1); 
		    double a = getTokenValue(pos+2);
		    double b = getTokenValue(pos+3);
    		
		    f3SetDecreaseRemove(pos, MathFunctions.cHi(x, a, b) );
    		
	    }	
    	
    	

	    /**
	     * Updating missing tokens (i.e. indexes i sum operator). Used when creating
	     * internal expressions based on the sublist of tokens.
	     * 
	     * 
	     * @param      tokens              the tokens list
	     * @param      keyWord             missing key word
	     * @param      tokenId             missing token id
	     * @param      tokenTypeId         missing token type id
	     */
	    private void updateMissingTokens(List<Token> tokens, String keyWord, int tokenId, int tokenTypeId) {
    		
		    foreach (Token t in tokens)
			    if ( (t.tokenTypeId == Const.NaN) && (t.tokenStr.Equals(keyWord))) {
				    t.keyWord = keyWord;
				    t.tokenId = tokenId;
				    t.tokenTypeId = tokenTypeId;
			    }
    		
	    }
    	
	    /**
	     * Summation operator (SIGMA by)
         * sum(i,m,n,f(i),b) --> sum f(i) from i=m to i=n by delta
         * i - index (argument)
         * m, n - numbers or expressions
         * f(i) - function string 
         * by delta
         *
	     * @param      pos                 the token position
	     */
	    private void SUM(int pos) {
    			
    		
		    List<FunctionParameter> sumParams = getFunctionParameters(pos, tokensList);
    		
    		
		    /*
		     * Get index string
		     * 1st parameter
		     */
		    FunctionParameter indexParam = sumParams[0];

		    /*
		     * Get from string (range from-to) 
		     * 2nd parameter
		     */
		    FunctionParameter fromParam = sumParams[1];

		    /*
		     * Get to string (range from-to) 
		     * 3rd parameter
		     */
		    FunctionParameter toParam = sumParams[2];
    		
    		
		    /*
		     * Get internal function strinng
		     * 4th - parameter
		     */		
		    FunctionParameter funParam = sumParams[3];		
    		
		    /*
		     * Get internal function strinng
		     * 5th - parameter
		     */		
    		
		    FunctionParameter deltaParam = null;
		    bool withDelta = false;
    		
		    if (sumParams.Count == 5) {
			    deltaParam = sumParams[4];
			    withDelta = true;			
		    }
    		
		    /*
		     * Create index argument
		     */
		    ArgumentParameter index = getParamArgument(indexParam.paramStr);
		    if (index.presence == Argument.NOT_FOUND) {
			    updateMissingTokens(indexParam.tokens, indexParam.paramStr, index.index, Argument.TYPE_ID );
			    updateMissingTokens(fromParam.tokens, indexParam.paramStr, index.index, Argument.TYPE_ID );
			    updateMissingTokens(toParam.tokens, indexParam.paramStr, index.index, Argument.TYPE_ID );
			    updateMissingTokens(funParam.tokens, indexParam.paramStr, index.index, Argument.TYPE_ID );
		    }
    		
    		
    		
		    /*
		     * Create from, to, fun expression
		     * based on the from string
		     *    expressions will use the same arguments list
		     *    as used in the main expression (this.argumentsList)
		     */
		    Expression fromExp = new Expression(fromParam.paramStr, fromParam.tokens, argumentsList, functionsList, constantsList);
		    Expression toExp = new Expression(toParam.paramStr, toParam.tokens, argumentsList, functionsList, constantsList);
		    Expression funExp = new Expression(funParam.paramStr, funParam.tokens, argumentsList, functionsList, constantsList);
		    Expression deltaExp = null;
    		
		    if (verboseMode == true) {
			    fromExp.setVerboseMode();
			    toExp.setVerboseMode();
			    funExp.setVerboseMode();
		    }
    		
		    /*
		     * Evaluate range
		     */
    				
		    double from = fromExp.calculate();
		    double to = toExp.calculate();
    		
		    double delta = 1;
		    if (to < from) delta = -1;
    		
		    if (withDelta == true) {
    			
			    deltaExp = new Expression(deltaParam.paramStr, deltaParam.tokens, argumentsList, functionsList, constantsList);
			    if (index.presence == Argument.NOT_FOUND) {
				    updateMissingTokens(deltaParam.tokens, indexParam.paramStr, index.index, Argument.TYPE_ID );
			    }			
    	
			    if (verboseMode == true)
				    deltaExp.setVerboseMode();
    			
			    delta = deltaExp.calculate();
    						
		    }
    		
		    //showTokens(funParam.tokens);
    				
		    /*
		     * Perform summary operation.
		     * i goes within the range (form-to by byExp)
		     * funExp value depends on the index vale
		     */
		    double sigma = NumberTheory.sigmaSummation(funExp, index.argument, from, to, delta);
    		
		    /*
		     * Clear index attribute
		     */
		    clearParamArgument(index);
    		
		    /*
		     * Call SetDecreaseRemove like method
		     * refere to f1SetDecreaseRemove()
		     */
		    calcSetDecreaseRemove(pos, sigma);
    		
	    }
    	
	    /**
	     * Product operator (SIGMA by)
         * pord(i,m,n,f(i),b) --> prod f(i) from i=m to i=n by delta
         * i - index (argument)
         * m, n - numbers or expressions
         * f(i) - function string 
         * by delta
         * 
	     * @param      pos                 the token position
	     */
	    private void PROD(int pos) {
    			
		    List<FunctionParameter> prodParams = getFunctionParameters(pos, tokensList);
    		
    		
		    /*
		     * Get index string
		     * 1st parameter
		     */
		    FunctionParameter indexParam = prodParams[0];

		    /*
		     * Get from string (range from-to) 
		     * 2nd parameter
		     */
		    FunctionParameter fromParam = prodParams[1];

		    /*
		     * Get to string (range from-to) 
		     * 3rd parameter
		     */
		    FunctionParameter toParam = prodParams[2];
    		
    		
		    /*
		     * Get internal function strinng
		     * 4th - parameter
		     */		
		    FunctionParameter funParam = prodParams[3];		
    		
		    /*
		     * Get internal function strinng
		     * 5th - parameter
		     */		
    		
		    FunctionParameter deltaParam = null;
		    bool withDelta = false;
    		
		    if (prodParams.Count == 5) {
			    deltaParam = prodParams[4];
			    withDelta = true;			
		    }
    		
		    /*
		     * Create index argument
		     */		
		    ArgumentParameter index = getParamArgument(indexParam.paramStr);
    		
		    if (index.presence == Argument.NOT_FOUND) {
			    updateMissingTokens(indexParam.tokens, indexParam.paramStr, index.index, Argument.TYPE_ID );
			    updateMissingTokens(fromParam.tokens, indexParam.paramStr, index.index, Argument.TYPE_ID );
			    updateMissingTokens(toParam.tokens, indexParam.paramStr, index.index, Argument.TYPE_ID );
			    updateMissingTokens(funParam.tokens, indexParam.paramStr, index.index, Argument.TYPE_ID );
		    }
    		
		    /*
		     * Create from, to, fun expression
		     * based on the from string
		     *    expressions will use the same arguments list
		     *    as used in the main expression (this.argumentsList)
		     */
		    Expression fromExp = new Expression(fromParam.paramStr, fromParam.tokens, argumentsList, functionsList, constantsList);
		    Expression toExp = new Expression(toParam.paramStr, toParam.tokens, argumentsList, functionsList, constantsList);
		    Expression funExp = new Expression(funParam.paramStr, funParam.tokens, argumentsList, functionsList, constantsList);
		    Expression deltaExp = null;
    		
		    if (verboseMode == true) {
			    fromExp.setVerboseMode();
			    toExp.setVerboseMode();
			    funExp.setVerboseMode();
		    }
    		

		    /*
		     * Evaluate range
		     */
    				
		    double from = fromExp.calculate();
		    double to = toExp.calculate();
    		
		    double delta = 1;
		    if (to < from) delta = -1;
    		
		    if (withDelta == true) {
    			
			    if (index.presence == Argument.NOT_FOUND)
				    updateMissingTokens(deltaParam.tokens, indexParam.paramStr, index.index, Argument.TYPE_ID );
    			
			    deltaExp = new Expression(deltaParam.paramStr, deltaParam.tokens, argumentsList, functionsList, constantsList);
    			
			    if (verboseMode == true)
				    deltaExp.setVerboseMode();
    			
			    delta = deltaExp.calculate();
    						
		    }
    		
		    //showTokens(funParam.tokens);
    				
		    /*
		     * Perform summary operation.
		     * i goes within the range (form-to by byExp)
		     * funExp value depends on the index vale
		     */
		    double product = NumberTheory.piProduct(funExp, index.argument, from, to, delta);
    		
		    /*
		     * clear index attribute
		     */
		    clearParamArgument(index);
    		
		    /*
		     * Call SetDecreaseRemove like method
		     * refere to f1SetDecreaseRemove()
		     */
		    calcSetDecreaseRemove(pos, product);
    		
	    }




    	
	    /*
	     * Function derivative
	     * 
	     * @param      pos                 the token position
	     * @param      derivativeType      the type of derivative (LEFT, RIGHT, ...)
	     */
	    private void DERIVATIVE(int pos, int derivativeType) {
    	
		    List<FunctionParameter> derParams = getFunctionParameters(pos, tokensList);

		    /*
		     * Default epsilon
		     */		
		    const double DEF_EPS		= 1E-8;
    		
		    /*
		     * Default max number of steps
		     */				
		    const int DEF_MAX_STEPS		= 20;
    		
		    /*
		     * Get internal function strinng
		     * 1th - parameter
		     */		
		    FunctionParameter funParam = derParams[0];
    		
		    /*
		     * Get argument
		     * 2nd - parameter
		     */		
		    FunctionParameter xParam = derParams[1];		
    		
		    ArgumentParameter x = getParamArgument(xParam.paramStr);
    		
		    if (x.presence == Argument.NOT_FOUND) {
			    updateMissingTokens(xParam.tokens, xParam.paramStr, x.index, Argument.TYPE_ID );		
			    updateMissingTokens(funParam.tokens, xParam.paramStr, x.index, Argument.TYPE_ID );		
		    }
    		
		    Expression funExp = new Expression(funParam.paramStr, funParam.tokens, argumentsList, functionsList, constantsList);
		    double x0 = x.argument.getArgumentValue();
    				
    		
		    double eps = DEF_EPS;
		    int maxSteps = DEF_MAX_STEPS;
    		
		    if (derParams.Count == 4) {
    			
			    FunctionParameter epsParam = derParams[2];		
			    FunctionParameter maxStepsParam = derParams[3];
    			
			    if (x.presence == Argument.NOT_FOUND) {
				    updateMissingTokens(epsParam.tokens, xParam.paramStr, x.index, Argument.TYPE_ID );		
				    updateMissingTokens(maxStepsParam.tokens, xParam.paramStr, x.index, Argument.TYPE_ID );		
			    }
    			
			    Expression epsExpr = new Expression(epsParam.paramStr, epsParam.tokens, argumentsList, functionsList, constantsList);			
			    Expression maxStepsExp = new Expression(maxStepsParam.paramStr, maxStepsParam.tokens, argumentsList, functionsList, constantsList);
    			
			    eps = epsExpr.calculate();
			    maxSteps = (int)Math.Round(maxStepsExp.calculate());
    			
		    }
    		
    		
		    if (derivativeType == NumericalAnalysis.GENERAL_DERIVATIVE) {
    			
			    double general = NumericalAnalysis.derivative(funExp, x.argument, x0, NumericalAnalysis.GENERAL_DERIVATIVE, eps, maxSteps);
			    calcSetDecreaseRemove(pos, general);
    			
		    } else if (derivativeType == NumericalAnalysis.LEFT_DERIVATIVE) {
    			
			    double left = NumericalAnalysis.derivative(funExp, x.argument, x0, NumericalAnalysis.LEFT_DERIVATIVE, eps, maxSteps);
			    calcSetDecreaseRemove(pos, left);
    			
		    } else {
    			
			    double right = NumericalAnalysis.derivative(funExp, x.argument, x0, NumericalAnalysis.RIGHT_DERIVATIVE, eps, maxSteps);
			    calcSetDecreaseRemove(pos, right);
    			
		    }

		    clearParamArgument(x);
    				
	    }
    	
	    /**
	     * Function derivative
	     * 
	     * @param      pos                 the token position
	     * @param      derivativeType      the type of derivative (left, right, etc...)
	     */
	    private void DERIVATIVE_NTH(int pos, int derivativeType) {

		    const double DEF_EPS		= 1E-6;
    		
		    /*
		     * Default max number of steps
		     */				
		    const int DEF_MAX_STEPS		= 20;
    		
		    List<FunctionParameter> derParams = getFunctionParameters(pos, tokensList);		
    		
		    /*
		     * Get internal function strinng
		     * 1st - parameter
		     */		
		    FunctionParameter funParam = derParams[0];

		    /*
		     * Get n-th
		     * 2nd - parameter
		     */		
		    FunctionParameter nParam = derParams[1];
    		
		    /*
		     * Get argument
		     * 3rd - parameter
		     */		
		    FunctionParameter xParam = derParams[2];
    					
		    ArgumentParameter x = getParamArgument(xParam.paramStr);
    		
		    if (x.presence == Argument.NOT_FOUND) {
			    updateMissingTokens(xParam.tokens, xParam.paramStr, x.index, Argument.TYPE_ID );
			    updateMissingTokens(funParam.tokens, xParam.paramStr, x.index, Argument.TYPE_ID );
			    updateMissingTokens(nParam.tokens, xParam.paramStr, x.index, Argument.TYPE_ID );
		    }
    		
		    Expression funExp = new Expression(funParam.paramStr, funParam.tokens, argumentsList, functionsList, constantsList);
		    Expression nExp = new Expression(nParam.paramStr, nParam.tokens, argumentsList, functionsList, constantsList);

		    double n = nExp.calculate();
		    double x0 = x.argument.getArgumentValue();
    		
    		
		    double eps = DEF_EPS;
		    int maxSteps = DEF_MAX_STEPS;
    		
		    if (derParams.Count == 5) {
    			
			    FunctionParameter epsParam = derParams[3];		
			    FunctionParameter maxStepsParam = derParams[4];
    			
			    if (x.presence == Argument.NOT_FOUND) {
				    updateMissingTokens(epsParam.tokens, xParam.paramStr, x.index, Argument.TYPE_ID );
				    updateMissingTokens(maxStepsParam.tokens, xParam.paramStr, x.index, Argument.TYPE_ID );
			    }
    			
			    Expression epsExpr = new Expression(epsParam.paramStr, epsParam.tokens, argumentsList, functionsList, constantsList);			
			    Expression maxStepsExp = new Expression(maxStepsParam.paramStr, maxStepsParam.tokens, argumentsList, functionsList, constantsList);
    			
			    eps = epsExpr.calculate();
			    maxSteps = (int)Math.Round(maxStepsExp.calculate());
    			
		    }
    		
    		
		    if (derivativeType == NumericalAnalysis.GENERAL_DERIVATIVE) {
    			
			    double left = NumericalAnalysis.derivativeNth(funExp, n, x.argument, x0, NumericalAnalysis.LEFT_DERIVATIVE, eps, maxSteps);
			    double right = NumericalAnalysis.derivativeNth(funExp, n, x.argument, x0, NumericalAnalysis.RIGHT_DERIVATIVE, eps, maxSteps);
			    calcSetDecreaseRemove(pos, (left + right) / 2.0);
    			
		    } else if (derivativeType == NumericalAnalysis.LEFT_DERIVATIVE) {
    			
			    double left = NumericalAnalysis.derivativeNth(funExp, n, x.argument, x0, NumericalAnalysis.LEFT_DERIVATIVE, eps, maxSteps);
			    calcSetDecreaseRemove(pos, left);
    			
		    } else {
    			
			    double right = NumericalAnalysis.derivativeNth(funExp, n, x.argument, x0, NumericalAnalysis.RIGHT_DERIVATIVE, eps, maxSteps);
			    calcSetDecreaseRemove(pos, right);
    			
		    }
    		
		    clearParamArgument(x);
    				
	    }
    	
    	
    		
	    /**
	     * Function integral
	     * 
	     * @param      pos                 the token position
	     */
	    private void INTEGRAL(int pos) {
    	
		    /**
		     * Default epsilon
		     */		
		    const double DEF_EPS		= 1E-6
		    ;
    		
		    /*
		     * Default max number of steps
		     */				
		    const int DEF_MAX_STEPS		= 20;

		    List<FunctionParameter> intParams = getFunctionParameters(pos, tokensList);		
    		
    		
		    /*
		     * Get internal function strinng
		     * 1th - parameter
		     */		
		    FunctionParameter funParam = intParams[0];
    		
		    /*
		     * Get argument
		     * 2nd - parameter
		     */		
		    FunctionParameter xParam = intParams[1];		

		    /*
		     * Get <a,b>
		     * 2nd - parameter
		     */		
		    FunctionParameter aParam = intParams[2];		
		    FunctionParameter bParam = intParams[3];		
    		
		    ArgumentParameter x = getParamArgument(xParam.paramStr);
    		
		    if (x.presence == Argument.NOT_FOUND) {
			    updateMissingTokens(xParam.tokens, xParam.paramStr, x.index, Argument.TYPE_ID );
			    updateMissingTokens(funParam.tokens, xParam.paramStr, x.index, Argument.TYPE_ID );
			    updateMissingTokens(aParam.tokens, xParam.paramStr, x.index, Argument.TYPE_ID );
			    updateMissingTokens(bParam.tokens, xParam.paramStr, x.index, Argument.TYPE_ID );
		    }
    		
		    Expression funExp = new Expression(funParam.paramStr, funParam.tokens, argumentsList, functionsList, constantsList);
		    Expression aExp = new Expression(aParam.paramStr, aParam.tokens, argumentsList, functionsList, constantsList);
		    Expression bExp = new Expression(bParam.paramStr, bParam.tokens, argumentsList, functionsList, constantsList);
    				
		    double eps = DEF_EPS;
		    int maxSteps = DEF_MAX_STEPS;
		    /*
		    if (argParam.parametersNumber == 4) {
    			
			    CalculusParameter epsParam = getCalculusParameter(pos,3);		
			    CalculusParameter maxStepsParam = getCalculusParameter(pos,4);	
    			
			    Expression epsExpr = new Expression(epsParam.paramStr, argumentsList);			
			    Expression maxStepsExp = new Expression(maxStepsParam.paramStr, argumentsList);
    			
			    eps = epsExpr.getExpressionValue();
			    maxSteps = (int)Math.round(maxStepsExp.getExpressionValue());
    			
		    } */
    		
		    calcSetDecreaseRemove(pos, NumericalAnalysis.integralTrapezoid(funExp, x.argument, aExp.calculate(), bExp.calculate(), eps, maxSteps) );
    					
		    clearParamArgument(x);
    		
	    }	
    	
    	
	    /**
	     * Forward difference operator
	     * 
	     * @param      pos                 the token position
	     */
	    private void FORWARD_DIFFERENCE(int pos) {
    		
		    List<FunctionParameter> parameters = getFunctionParameters(pos, tokensList);		
    		
		    FunctionParameter funParam = parameters[0];
		    FunctionParameter xParam = parameters[1];
    		
		    ArgumentParameter x = getParamArgument(xParam.paramStr);
    				
		    Expression funExp = new Expression(funParam.paramStr, funParam.tokens, argumentsList, functionsList, constantsList);

		    if (verboseMode == true)
			    funExp.setVerboseMode();
    		
		    double h = 1;
    		
		    if (parameters.Count == 3) {

                FunctionParameter hParam = parameters[2];
			    Expression hExp = new Expression(hParam.paramStr, hParam.tokens, argumentsList, functionsList, constantsList);
    			
			    if (verboseMode == true)
				    hExp.setVerboseMode();
    			
			    h = hExp.calculate();
    			
		    }
    		
		    calcSetDecreaseRemove(pos, NumberTheory.forwardDifference(funExp, h, x.argument) );
    		
		    clearParamArgument(x);
    		
	    }
    	
	    /**
	     * Backward diffrence operator
	     * 
	     * @param      pos                 the token position
	     */
	    private void BACKWARD_DIFFERENCE(int pos) {
    		
		    List<FunctionParameter> parameters = getFunctionParameters(pos, tokensList);		
    		
		    FunctionParameter funParam = parameters[0];
		    FunctionParameter xParam = parameters[1];
    		
		    ArgumentParameter x = getParamArgument(xParam.paramStr);
    				
		    Expression funExp = new Expression(funParam.paramStr, funParam.tokens, argumentsList, functionsList, constantsList);

		    if (verboseMode == true)
			    funExp.setVerboseMode();
    		
		    double h = 1;
    		
		    if (parameters.Count == 3) {

                FunctionParameter hParam = parameters[2];
			    Expression hExp = new Expression(hParam.paramStr, hParam.tokens, argumentsList, functionsList, constantsList);
    			
			    if (verboseMode == true)
				    hExp.setVerboseMode();
    			
			    h = hExp.calculate();
    			
		    }
    		
		    calcSetDecreaseRemove(pos, NumberTheory.backwardDifference(funExp, h, x.argument) );
    		
		    clearParamArgument(x);
    		
	    }	
    	
    	
	    /**
	     * Minimum
	     * Sets tokens to number token
	     * 
	     * @param      pos                 the token position
	     */		
	    private void MIN(int pos) {
    		
		    List<Double> numbers = getNumbers(pos);		
		    specSetDecreaseRemove(pos, MathFunctions.min( mXparser.arraList2double(numbers) ), numbers.Count );

	    }
    	
	    /**
	     * Maximum
	     * Sets tokens to number token
	     * 
	     * @param pos token index (position)
	     */		
	    private void MAX(int pos) {

		    List<Double> numbers = getNumbers(pos);		
		    specSetDecreaseRemove(pos, MathFunctions.max( mXparser.arraList2double(numbers) ), numbers.Count );

	    }

	    /**
	     * Continued fraction
	     * 
	     * @param      pos                 the token position
	     */
	    private void CONTINUED_FRACTION(int pos) {
    		
		    List<Double> numbers = getNumbers(pos);		
		    specSetDecreaseRemove(pos, MathFunctions.continuedFraction( mXparser.arraList2double(numbers) ), numbers.Count );
    				
	    }
    	
	    /**
	     * Continued polynomial
	     * 
	     * @param      pos                 the token position
	     */
	    private void CONTINUED_POLYNOMIAL(int pos) {
    		
		    List<Double> numbers = getNumbers(pos);		
		    specSetDecreaseRemove(pos, MathFunctions.continuedPolynomial( mXparser.arraList2double(numbers) ), numbers.Count );
    				
	    }
    	
	    /**
	     * Greates Common Divisor
	     * 
	     * @param      pos                 the token position
	     */
	    private void GCD(int pos) {
    		
		    List<Double> numbers = getNumbers(pos);		
		    specSetDecreaseRemove(pos, MathFunctions.gcd( mXparser.arraList2double(numbers) ), numbers.Count );
    				
	    }
    	
	    /**
	     * Lowest Common Multiply
	     * 
	     * @param      pos                 the token position
	     */
	    private void LCM(int pos) {
    		
		    List<Double> numbers = getNumbers(pos);		
		    specSetDecreaseRemove(pos, MathFunctions.lcm( mXparser.arraList2double(numbers) ), numbers.Count );
    				
	    }
    	
	    /**
	     * Parser symbols
	     * Removes comma
	     * 
	     * @param pos token index (position)
	     */		
	    private void COMMA(int pos) {

		    tokensList.RemoveAt(pos);

	    }	
    	
	    /**
	     * Parser symbols
	     * Removes parenthesis
	     * 
	     * @param lPos    left token index (position)
	     * @param rPos    roght token index (position)
	     */		
	    private void PARENTHESES(int lPos, int rPos) {
    		
		    for (int p = lPos; p <= rPos; p++)
			    tokensList[p].tokenLevel--;
    		
		    tokensList.RemoveAt(rPos);
		    tokensList.RemoveAt(lPos);
    		
	    }

    	
    	
    	
	    /*=================================================
	     * 
	     * Syntax checking and calculate() method 
	     * 
	     *================================================= 
	     */
    	
    	
	    /**
	     * Checks syntax of the expression string.
	     * 
	     * @return     true if syntax is ok
	     */
	    public bool checkSyntax() {
    				
		    bool syntax = checkSyntax("[" + expressionString + "] ");		
		    return syntax;

	    }	

	    /**
	     * Checks syntax of the calculus parameter
	     * 
	     * @return     true if syntax is ok
	     */
	    private int checkCalculusParameter(String param) {
    		
		    int errors = 0;
    		
		    foreach (KeyWord kw in keyWordsList)
			    if (kw.wordTypeId != Argument.TYPE_ID)
				    if ( param.Equals(kw.wordString) )
					    errors++;
    		
		    return errors;
	    }
    	
	    /**
	     * Checks if argument given in the function parameter is known
	     * in the expression.
	     * 
	     * @param      param               the function parameter 
	     * 
	     * @return     true if argument is known,
	     *             otherwise returns false.
	     */
	    private bool checkIfKnownArgument(FunctionParameter param) {
    		
		    if (param.tokens.Count > 1)
			    return false;
    		
		    Token t = param.tokens[0];
    		
		    if (t.tokenTypeId != Argument.TYPE_ID)
			    return false;
    		
    		
		    return true;
    		
	    }
    	
	    /**
	     * Checks if token i suknown
	     * 
	     * @param      param               the function parameter 
	     * 
	     * @return     true if there is only 1 token with unknown type,
	     *             otherwise returns false.
	     */	
	    private bool checkIfUnknownToken(FunctionParameter param) {
    		
		    if (param.tokens.Count > 1)
			    return false;
    		
		    Token t = param.tokens[0];
    		
		    if (t.tokenTypeId != Const.NaN)
			    return false;
    		
    		
		    return true;
    		
	    }	
	    /**
	     * Checking the syntax (recursively).
	     * 
	     * @param      level               string representing the recurssion level.
	     * @return     true if syntax was correct,
	     *             otherwise returns false.
	     */
	    private bool checkSyntax(String level) {
    		
		    if ( (expressionWasModified == false) && (syntaxStatus == NO_SYNTAX_ERRORS) ) {
			    errorMessage = level + "already checked - no errors!\n";
                recursionCallPending = false;
                return NO_SYNTAX_ERRORS;
		    }

            recursionCallPending = true;
            errorMessage = level +"checking ...\n";
		    bool syntax = NO_SYNTAX_ERRORS;

            
            SyntaxChecker syn = new SyntaxChecker(new MemoryStream(Encoding.ASCII.GetBytes(expressionString)) );
	        try {
    	    	
	            syn.checkSyntax();
	            /*
	             * IF there are no lex error
	             */
			    tokenizeExpressionString();
    			
			    String kw1;
			    String kw2;
    			
			    keyWordsList.Sort( new KwStrComparator() );
    			
			    for (int kwId = 1; kwId < keyWordsList.Count; kwId++) {
    				
				    kw1 = keyWordsList[kwId-1].wordString;
				    kw2 = keyWordsList[kwId].wordString;
    				
				    if ( kw1.Equals(kw2) ) {
					    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
					    errorMessage = errorMessage + level + "(" + kw1 + ") Duplicated <KEYWORD>.\n";					
				    }
    					
			    }
    		
			    int tokensNumber = initialTokens.Count;
    			
			    Stack<SyntaxStackElement> syntaxStack = new Stack<SyntaxStackElement>();
			    SyntaxStackElement stackElement; 
    			
			    for (int tokenIndex = 0; tokenIndex < tokensNumber; tokenIndex++ ) {
				    Token t = initialTokens[tokenIndex];
				    String tokenStr = "(" + t.tokenStr +", " + tokenIndex + ") ";

				    if (t.tokenTypeId == Argument.TYPE_ID) {
    				
					    Argument arg = getArgument(t.tokenId);
    						
					    if ( arg.getArgumentType() == Argument.DEPENDENT_ARGUMENT ) {
    						
						    if (getParametersNumber(tokenIndex) >= 0 ) {
    							
							    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
							    errorMessage = errorMessage + level + tokenStr + "<ARGUMENT> was expected.\n";
    							
						    } else
    							
							    if ( (arg.argumentExpression != this) && (arg.argumentExpression.recursionCallPending == false) ) {
    							
								    bool syntaxRec = arg.argumentExpression.checkSyntax(level + "-> " + "[" + t.tokenStr + "] = [" + arg.argumentExpression.getExpressionString() + "] ");
								    syntax = syntax && syntaxRec;
								    errorMessage = errorMessage + level + tokenStr + "checking dependent argument ...\n" + arg.argumentExpression.getErrorMessage();
							    }
    						
					    } 
    				
				    }

				    if (t.tokenTypeId == RecursiveArgument.TYPE_ID) {
    					
					    Argument arg = getArgument(t.tokenId);
    						
    						
					    if (getParametersNumber(tokenIndex) != 1 ) {
    							
						    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
						    errorMessage = errorMessage + level + tokenStr + "<RECURSIVE_ARGUMENT> expecting 1 parameter.\n";
    							
					    } else
    						
						    if ( (arg.argumentExpression != this) && (arg.argumentExpression.recursionCallPending == false) ) {
    						
							    bool syntaxRec = arg.argumentExpression.checkSyntax(level + "-> " + "[" + t.tokenStr + "] = [" + arg.argumentExpression.getExpressionString() + "] ");
							    syntax = syntax && syntaxRec;
							    errorMessage = errorMessage + level + tokenStr + "checking recursive argument ...\n" + arg.argumentExpression.getErrorMessage();
						    }
    						
    				
				    }				
    				
				    if (t.tokenTypeId == Const.NaN) {

					    bool calculusToken = false;
    					
					    foreach (SyntaxStackElement e in syntaxStack)
						    if ( e.tokenStr.Equals(t.tokenStr) )
							    calculusToken = true;
    					
    					
					    if (!calculusToken) {
						    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
						    errorMessage = errorMessage + level + tokenStr + "invalid <TOKEN>.\n";
					    }
    										
				    }								
    				
				    if (t.tokenTypeId == Function.TYPE_ID) {
    				
					    Function fun = getFunction(t.tokenId);
    					
					    if ( fun.getParametersNumber() != getParametersNumber(tokenIndex) ) {
						    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
						    errorMessage = errorMessage + level + tokenStr + "<USER_DEFINED_FUNCTION> expecting " + fun.getParametersNumber() + " arguments.\n";
    						
					    } else
    						
						    if ( (fun.functionExpression != this) && (fun.functionExpression.recursionCallPending == false) ) {
    							
							    bool syntaxRec = fun.functionExpression.checkSyntax(level + "-> " + "[" + t.tokenStr + "] = [" + fun.functionExpression.getExpressionString() + "] ");
							    syntax = syntax && syntaxRec;
							    errorMessage = errorMessage + level + tokenStr + "checking user defined function ...\n" + fun.functionExpression.getErrorMessage();
    						
						    }
    				
				    }
    				
				    if (t.tokenTypeId == Const.TYPE_ID) {
    					
					    if ( getParametersNumber(tokenIndex) >= 0 ) {
						    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
						    errorMessage = errorMessage + level + tokenStr + "<CONSTANT> was expected.\n";
					    }
    					
    					
				    }

				    if (t.tokenTypeId == Constant.TYPE_ID) {
    					
					    if ( getParametersNumber(tokenIndex) >= 0 ) {
						    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
						    errorMessage = errorMessage + level + tokenStr + "<USER_DEFINED_CONSTANT> was expected.\n";
					    }
    					
    					
				    }
    				
    				
				    if (t.tokenTypeId == Function1Arg.TYPE_ID) {
    					
					    if ( getParametersNumber(tokenIndex) != 1 ) {
						    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
						    errorMessage = errorMessage + level + tokenStr + "<FUNCTION> expecting 1 argument.\n";							
					    }
    										
				    }
    				
				    if (t.tokenTypeId == Function2Arg.TYPE_ID) {
    					
					    if ( getParametersNumber(tokenIndex) != 2 ) {
						    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
						    errorMessage = errorMessage + level + tokenStr + "<FUNCTION> expecting 2 arguments.\n";							
					    }
    										
				    }				

				    if (t.tokenTypeId == Function3Arg.TYPE_ID) {
    					
					    if ( getParametersNumber(tokenIndex) != 3 ) {
						    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
						    errorMessage = errorMessage + level + tokenStr + "<FUNCTION> expecting 3 arguments.\n";							
					    }
    										
				    }
    				
				    if (t.tokenTypeId == Calculus.TYPE_ID) {
    					
					    int paramsNumber = getParametersNumber(tokenIndex);
					    List<FunctionParameter> funParams = null;
    					
					    if (paramsNumber > 0)
						    funParams = getFunctionParameters(tokenIndex, initialTokens);
    					
					    if ( (t.tokenId == Calculus.DER_ID) || (t.tokenId == Calculus.DER_LEFT_ID) || (t.tokenId == Calculus.DER_RIGHT_ID) )  {
    						
						    if ( (paramsNumber !=2) && (paramsNumber != 4) ) {
							    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
							    errorMessage = errorMessage + level + tokenStr + "<DERIVATIVE> expecting 2 or 4 calculus arguments.\n";							
						    } else {
    							
							    FunctionParameter argParam = funParams[1];
    							
							    if ( checkIfKnownArgument(argParam) == false) {
    									
								    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
								    errorMessage = errorMessage + level + tokenStr + "<DERIVATIVE> argument was expected.\n";
    									
							    }
    								
						    }
    							
    						
					    }
    					

				    /*
					    if (t.tokenId == Calculus.DER_LEFT_ID) {
    						
						    if ( (paramsNumber !=2) && (paramsNumber != 4) ) {
							    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
							    errorMessage = errorMessage + level + tokenStr + "<LEFT_DERIVATIVE> expecting 2 or 4 calculus arguments.\n";							
						    } else {
    						
							    FunctionParameter argParam = funParams.get(1);
    							
							    if ( checkIfKnownArgument(argParam) == false) {
    								
								    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
								    errorMessage = errorMessage + level + tokenStr + "<DERIVATIVE> argument was expected.\n";
    								
							    }

    							
						    }
    						
					    }
    					
					    if (t.tokenId == Calculus.DER_RIGHT_ID) {
    						
						    if ( (paramsNumber !=2) && (paramsNumber != 4) ) {
							    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
							    errorMessage = errorMessage + level + tokenStr + "<RIGTH_DERIVATIVE> expecting 2 or 4 calculus arguments.\n";							
						    } else {
    						
							    FunctionParameter argParam = funParams.get(1);
    							
							    if ( checkIfKnownArgument(argParam) == false) {
    								
								    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
								    errorMessage = errorMessage + level + tokenStr + "<DERIVATIVE> argument was expected.\n";
    								
							    }
    							
    							
						    }
    						
					    }
    				
				    */
    				
					    if (t.tokenId == Calculus.DERN_ID) {
    						
						    if ( (paramsNumber !=3) && (paramsNumber != 5) ) {
							    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
							    errorMessage = errorMessage + level + tokenStr + "<NTH_DERIVATIVE> expecting 3 or 5 calculus arguments.\n";							
						    } else {
    						
							    FunctionParameter argParam = funParams[2];
    							
							    if ( checkIfKnownArgument(argParam) == false) {
    								
								    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
								    errorMessage = errorMessage + level + tokenStr + "<DERIVATIVE> argument was expected.\n";
    								
							    }

						    }
    						
					    }
    					
					    if (t.tokenId == Calculus.INT_ID) {
    						
						    if (paramsNumber !=4) {
							    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
							    errorMessage = errorMessage + level + tokenStr + "<INTEGRAL> expecting 4 calculus arguments.\n";							
						    } else {
    						
							    FunctionParameter argParam = funParams[1];
    							
							    stackElement = new SyntaxStackElement(argParam.paramStr, t.tokenLevel+1);
							    syntaxStack.Push(stackElement);
    							
							    int errors = checkCalculusParameter(stackElement.tokenStr);
    							
							    if (errors > 0) {
    								
								    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
								    errorMessage = errorMessage + level + tokenStr + "Found duplicated key words for calculus parameter " + "(" + stackElement.tokenStr + ", " + errors + ").\n";							
    								
							    }
    							
							    if ( !checkIfKnownArgument(argParam) && !checkIfUnknownToken(argParam) ) {
    								
								    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
								    errorMessage = errorMessage + level + tokenStr + "One token (argument or unknown) was expected.\n";							
    								
							    }
    							
    							
						    }
    						
					    }
					    if (t.tokenId == Calculus.PROD_ID) {
    						
						    if ( (paramsNumber != 4) && (paramsNumber != 5) ) {
							    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
							    errorMessage = errorMessage + level + tokenStr + "<PRODUCT> expecting 4 or 5 calculus arguments.\n";							
						    } else {
    						
							    FunctionParameter indexParam = funParams[0];
    							
							    stackElement = new SyntaxStackElement(indexParam.paramStr, t.tokenLevel+1);
							    syntaxStack.Push(stackElement);
    							
							    int errors = checkCalculusParameter(stackElement.tokenStr);
    							
							    if (errors > 0) {
    								
								    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
								    errorMessage = errorMessage + level + tokenStr + "Found duplicated key words for calculus parameter " + "(" + stackElement.tokenStr + ", " + errors + ").\n";							
    								
							    }
    							
							    if ( !checkIfKnownArgument(indexParam) && !checkIfUnknownToken(indexParam) ) {
    								
								    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
								    errorMessage = errorMessage + level + tokenStr + "One token (argument or unknown) was expected.\n";							
    								
							    }
    							
						    }
    						
					    }
    					
					    if (t.tokenId == Calculus.SUM_ID) {
    						
						    if ( (paramsNumber != 4) && (paramsNumber != 5) ) {
							    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
							    errorMessage = errorMessage + level + tokenStr + "<SUMMATION> expecting 4 or 5 calculus arguments.\n";							
						    } else {
    						
							    FunctionParameter indexParam = funParams[0];
    							
							    stackElement = new SyntaxStackElement(indexParam.paramStr, t.tokenLevel+1);
							    syntaxStack.Push(stackElement);
    							
							    int errors = checkCalculusParameter(stackElement.tokenStr);
    							
							    if (errors > 0) {
    								
								    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
								    errorMessage = errorMessage + level + tokenStr + "Found duplicated key words for calculus parameter " + "(" + stackElement.tokenStr + ", " + errors + ").\n";							
    								
							    }
    							
							    if ( !checkIfKnownArgument(indexParam) && !checkIfUnknownToken(indexParam) ) {
    								
								    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
								    errorMessage = errorMessage + level + tokenStr + "One token (argument or unknown) was expected.\n";							
    								
							    }
    							
    							
						    }
    						
					    }
    						
					    if ( (t.tokenId == Calculus.FORW_DIFF_ID) || (t.tokenId == Calculus.BACKW_DIFF_ID) ) {
    						
						    if ( (paramsNumber != 2) && (paramsNumber != 3) ) {
							    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
							    errorMessage = errorMessage + level + tokenStr + "<DIFF> expecting 2 or 3 arguments.\n";							
						    } else {
    						
							    FunctionParameter xParam = funParams[1];
    													
							    if ( checkIfKnownArgument(xParam) == false) {
    								
								    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
								    errorMessage = errorMessage + level + tokenStr + "<DIFF> argument was expected.\n";
    								
							    }
    							
    							
						    }
    						
					    }
    					
    					
					    /*
					    if (t.tokenId == Calculus.BACKW_DIFF_ID) {
    						
						    if ( (paramsNumber != 2) && (paramsNumber != 3) ) {
							    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
							    errorMessage = errorMessage + level + tokenStr + "<BACKW_DIFF> expecting 2 or 3 arguments.\n";							
						    } else {
    						
							    FunctionParameter xParam = funParams.get(1);
    													
							    if ( checkIfKnownArgument(xParam) == false) {
    								
								    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
								    errorMessage = errorMessage + level + tokenStr + "<BACKW_DIFF> argument was expected.\n";
    								
							    }
    							
    							
						    }
    						
					    }
					    */
    					
				    }

				    if (t.tokenTypeId == SpecialFunction.TYPE_ID) {
    					
					    int paramsNumber = getParametersNumber(tokenIndex);
    					
					    if ( 	(t.tokenId == SpecialFunction.CONT_FRAC_ID)
							    || (t.tokenId == SpecialFunction.CONT_POL_ID)
							    || (t.tokenId == SpecialFunction.LCM_ID)
							    || (t.tokenId == SpecialFunction.MAX_ID)
							    || (t.tokenId == SpecialFunction.MIN_ID)
    							
							    ) {
    						
    						
						    if (paramsNumber < 1) {
    							
							    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
							    errorMessage = errorMessage + level + tokenStr + "At least one argument was expected.\n";
    							
						    }
    						
    						
					    }
    					
					    if (t.tokenId == SpecialFunction.IFF_ID) {
    						
						    if ( (paramsNumber % 2 != 0) || (paramsNumber < 2) ) {
    							
							    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
							    errorMessage = errorMessage + level + tokenStr + "Expecting parity number of arguments.\n";
    							
						    }
    						
    						
					    }
    					
				    }
    				
				    if ( (t.tokenTypeId == ParserSymbol.TYPE_ID) && (t.tokenId == ParserSymbol.RIGHT_PARENTHESES_ID) ) {
    					
					    if ( syntaxStack.Count > 0 )
						    if (t.tokenLevel == syntaxStack.Peek().tokenLevel )
							    syntaxStack.Pop();
    					
				    }			

    				
			    }
    	

    	        
	        } catch (Exception e) {
    	    	
	    	    syntax = SYNTAX_ERROR_OR_STATUS_UNKNOWN;
      
                errorMessage = errorMessage + level + "lexical error \n\n" + e.Message + "\n";
    	    	
	        }		


		    if (syntax == NO_SYNTAX_ERRORS) {
			    errorMessage = errorMessage + level + "no errors.\n";
			    expressionWasModified = false;
		    } else {
			    errorMessage = errorMessage + level + "errors were found.\n";				
			    expressionWasModified = true;
		    }
		    syntaxStatus = syntax;

            recursionCallPending = false;
            return syntax;
    	    
	    }

        /**
         * Calculates the expression value
         * 
         * @return     The expression value if syntax was ok,
         *             otherwise returns Double.NaN.
         */
        public double calculate() {


            computingTime = 0;
            long startTime = mXparser.currentTimeMillis();


            if (verboseMode == true) {
                printSystemInfo("\n", NO_EXP_STR);
                printSystemInfo("\n", WITH_EXP_STR);
                printSystemInfo("Starting ...\n", WITH_EXP_STR);
                showArguments();
            }


            /* 
             * check expression syntax and
             * evaluate expression string tokens
             * 
             */

            if ((expressionWasModified == true) || (syntaxStatus != NO_SYNTAX_ERRORS))
                syntaxStatus = checkSyntax();

            if (syntaxStatus == SYNTAX_ERROR_OR_STATUS_UNKNOWN)
                return Double.NaN;

            copyInitialTokens();

            /*
             * if nothing to calculate return Double.NaN 
             */
            if (tokensList.Count == 0)
                return Double.NaN;


            /*
             * position for particular tokens types
             */
            //int calculusCounter = 0;
            int calculusPos;
            int ifPos;
            int iffPos;
            int specFunPos;
            int recArgPos;
            int f3ArgPos;
            int f2ArgPos;
            int f1ArgPos;
            int userFunPos;
            int plusPos;
            int minusPos;
            int multiplyPos;
            int dividePos;
            int powerPos;
            int powerNum;
            int factPos;
            int modPos;

            int negPos;
            int bolPos;

            int eqPos;
            int neqPos;
            int ltPos;
            int gtPos;
            int leqPos;
            int geqPos;
            int commaPos;
            int lParPos;
            int rParPos;

            Token token;

            int tokensNumber;
            int maxPartLevel;
            int lPos;
            int rPos;

            int tokenIndex;
            int pos;

            int p;
            List<int> commas = null;



            /* While exist token which needs to bee evaluated */
            do {
                //wait(1);

                //showTokens();

                tokensNumber = tokensList.Count;
                maxPartLevel = -1;
                lPos = -1;
                rPos = -1;

                /*
                 * initializing tokens types positions
                 */
                calculusPos = -1;
                ifPos = -1;
                iffPos = -1;
                specFunPos = -1;
                recArgPos = -1;
                f3ArgPos = -1;
                f2ArgPos = -1;
                f1ArgPos = -1;
                userFunPos = -1;
                plusPos = -1;
                minusPos = -1;
                multiplyPos = -1;
                dividePos = -1;
                powerPos = -1;
                factPos = -1;
                modPos = -1;
                powerNum = 0;
                negPos = -1;
                bolPos = -1;
                eqPos = -1;
                neqPos = -1;
                ltPos = -1;
                gtPos = -1;
                leqPos = -1;
                geqPos = -1;
                commaPos = -1;

                lParPos = -1;
                rParPos = -1;


                tokensNumber = tokensList.Count;


                /* calculus operations ... */
                p = -1;
                do {
                    p++;
                    token = tokensList[p];
                    if (token.tokenTypeId == Calculus.TYPE_ID)
                        calculusPos = p;
                } while ((p < tokensNumber - 1) && (calculusPos < 0));


                /* if operations ... */
                if (calculusPos < 0) {
                    p = -1;
                    do {
                        p++;
                        token = tokensList[p];
                        if ((token.tokenTypeId == Function3Arg.TYPE_ID) && (token.tokenId == Function3Arg.IF_CONDITION_ID))
                            ifPos = p;
                    } while ((p < tokensNumber - 1) && (ifPos < 0));
                }


                /* iff operations ... */

                if ((calculusPos < 0) && (ifPos < 0)) {
                    p = -1;
                    do {
                        p++;
                        token = tokensList[p];
                        if ((token.tokenTypeId == SpecialFunction.TYPE_ID) && (token.tokenId == SpecialFunction.IFF_ID))
                            iffPos = p;
                    } while ((p < tokensNumber - 1) && (iffPos < 0));
                }

                if ((calculusPos < 0) && (ifPos < 0) && (iffPos < 0)) {

                    /* Find start index of the tokens with the highest level */
                    for (tokenIndex = 0; tokenIndex < tokensNumber; tokenIndex++) {

                        token = tokensList[tokenIndex];

                        if (token.tokenLevel > maxPartLevel) {
                            maxPartLevel = tokensList[tokenIndex].tokenLevel;
                            lPos = tokenIndex;
                        }

                        if (token.tokenTypeId == Argument.TYPE_ID)
                            ARGUMENT(tokenIndex);
                        else
                            if (token.tokenTypeId == Const.TYPE_ID)
                                CONSTANT(tokenIndex);
                            else
                                if (token.tokenTypeId == Constant.TYPE_ID)
                                    USER_CONSTANT(tokenIndex);

                    }

                    tokenIndex = lPos;

                    /* Find end index of the tokens with the highest level */
                    while ((tokenIndex < tokensNumber) && (maxPartLevel == tokensList[tokenIndex].tokenLevel))
                        tokenIndex++;
                    rPos = tokenIndex - 1;

                    if (verboseMode == true) {
                        printSystemInfo("Parsing (" + lPos + ", " + rPos + ") ", WITH_EXP_STR);
                        showParsing(lPos, rPos);
                    }

                    /* if no calculus operations were found
                     * check for other tokens
                     */
                    for (pos = lPos; pos <= rPos; pos++) {

                        token = tokensList[pos];
                        if ((token.tokenTypeId == RecursiveArgument.TYPE_ID) && (recArgPos < 0))
                            recArgPos = pos;
                        else
                            if ((token.tokenTypeId == SpecialFunction.TYPE_ID) && (specFunPos < 0))
                                specFunPos = pos;
                            else
                                if ((token.tokenTypeId == Function3Arg.TYPE_ID) && (f3ArgPos < 0))
                                    f3ArgPos = pos;
                                else
                                    if ((token.tokenTypeId == Function2Arg.TYPE_ID) && (f2ArgPos < 0))
                                        f2ArgPos = pos;
                                    else
                                        if ((token.tokenTypeId == Function1Arg.TYPE_ID) && (f1ArgPos < 0))
                                            f1ArgPos = pos;
                                        else
                                            if ((token.tokenTypeId == Function.TYPE_ID) && (userFunPos < 0))
                                                userFunPos = pos;
                                            else
                                                if (token.tokenTypeId == Operator.TYPE_ID) {

                                                    if (token.tokenId == Operator.POWER_ID) {
                                                        powerPos = pos;
                                                        powerNum++;
                                                    } else
                                                        if ((token.tokenId == Operator.FACT_ID) && (factPos < 0)) {
                                                            factPos = pos;
                                                        } else
                                                            if ((token.tokenId == Operator.MOD_ID) && (modPos < 0)) {
                                                                modPos = pos;
                                                            } else
                                                                if ((token.tokenId == Operator.PLUS_ID) && (plusPos < 0))
                                                                    plusPos = pos;
                                                                else
                                                                    if ((token.tokenId == Operator.MINUS_ID) && (minusPos < 0))
                                                                        minusPos = pos;
                                                                    else
                                                                        if ((token.tokenId == Operator.MULTIPLY_ID) && (multiplyPos < 0))
                                                                            multiplyPos = pos;
                                                                        else
                                                                            if ((token.tokenId == Operator.DIVIDE_ID) && (dividePos < 0))
                                                                                dividePos = pos;

                                                } else
                                                    if ((token.tokenTypeId == BooleanOperator.TYPE_ID) && (token.tokenId == BooleanOperator.NEG_ID) && (negPos < 0)) {
                                                        negPos = pos;
                                                    } else
                                                        if ((token.tokenTypeId == BooleanOperator.TYPE_ID) && (bolPos < 0)) {
                                                            bolPos = pos;
                                                        } else
                                                            if (token.tokenTypeId == BinaryRelation.TYPE_ID) {

                                                                if ((token.tokenId == BinaryRelation.EQ_ID) && (eqPos < 0))
                                                                    eqPos = pos;
                                                                else
                                                                    if ((token.tokenId == BinaryRelation.NEQ_ID) && (neqPos < 0))
                                                                        neqPos = pos;
                                                                    else
                                                                        if ((token.tokenId == BinaryRelation.LT_ID) && (ltPos < 0))
                                                                            ltPos = pos;
                                                                        else
                                                                            if ((token.tokenId == BinaryRelation.GT_ID) && (gtPos < 0))
                                                                                gtPos = pos;
                                                                            else
                                                                                if ((token.tokenId == BinaryRelation.LEQ_ID) && (leqPos < 0))
                                                                                    leqPos = pos;
                                                                                else
                                                                                    if ((token.tokenId == BinaryRelation.GEQ_ID) && (geqPos < 0))
                                                                                        geqPos = pos;

                                                            } else
                                                                if (token.tokenTypeId == ParserSymbol.TYPE_ID) {

                                                                    if ((token.tokenId == ParserSymbol.COMMA_ID)) {

                                                                        if (commaPos < 0)
                                                                            commas = new List<int>();

                                                                        commas.Add(pos);
                                                                        commaPos = pos;

                                                                    } else
                                                                        if ((token.tokenId == ParserSymbol.LEFT_PARENTHESES_ID) && (lParPos < 0))
                                                                            lParPos = pos;
                                                                        else
                                                                            if ((token.tokenId == ParserSymbol.RIGHT_PARENTHESES_ID) && (rParPos < 0))
                                                                                rParPos = pos;

                                                                }
                    }

                    /*
                     * powering should be done using backwards sequence
                     */
                    if (powerNum > 1) {

                        powerPos = -1;

                        p = rPos + 1;

                        do {

                            p--;
                            token = tokensList[p];
                            if ((token.tokenTypeId == Operator.TYPE_ID) && (token.tokenId == Operator.POWER_ID))
                                powerPos = p;

                        } while ((p > lPos) && (powerPos == -1));

                    }

                }


                /* calculus operations */
                if (calculusPos >= 0) {

                    switch (tokensList[calculusPos].tokenId) {

                        case Calculus.SUM_ID: SUM(calculusPos);
                            break;
                        case Calculus.PROD_ID: PROD(calculusPos);
                            break;
                        case Calculus.INT_ID: INTEGRAL(calculusPos);
                            break;
                        case Calculus.DER_ID: DERIVATIVE(calculusPos, NumericalAnalysis.GENERAL_DERIVATIVE);
                            break;
                        case Calculus.DER_LEFT_ID: DERIVATIVE(calculusPos, NumericalAnalysis.LEFT_DERIVATIVE);
                            break;
                        case Calculus.DER_RIGHT_ID: DERIVATIVE(calculusPos, NumericalAnalysis.RIGHT_DERIVATIVE);
                            break;
                        case Calculus.DERN_ID: DERIVATIVE_NTH(calculusPos, NumericalAnalysis.GENERAL_DERIVATIVE);
                            break;
                        case Calculus.FORW_DIFF_ID: FORWARD_DIFFERENCE(calculusPos);
                            break;
                        case Calculus.BACKW_DIFF_ID: BACKWARD_DIFFERENCE(calculusPos);
                            break;

                    }

                } else
                    if (ifPos >= 0) {

                        IF_CONDITION(ifPos);

                    } else
                        if (iffPos >= 0) {

                            IFF(iffPos);

                        } else				/* ... arguments ... */
                            /* ... recursive arguments ... */
                            if (recArgPos >= 0) {

                                RECURSIVE_ARGUMENT(recArgPos);

                            } else
                                /* ... special functions  ... */
                                if (specFunPos >= 0) {

                                    switch (tokensList[specFunPos].tokenId) {

                                        case SpecialFunction.IFF_ID: IFF(specFunPos);
                                            break;
                                        case SpecialFunction.MIN_ID: MIN(specFunPos);
                                            break;
                                        case SpecialFunction.MAX_ID: MAX(specFunPos);
                                            break;
                                        case SpecialFunction.CONT_FRAC_ID: CONTINUED_FRACTION(specFunPos);
                                            break;
                                        case SpecialFunction.CONT_POL_ID: CONTINUED_POLYNOMIAL(specFunPos);
                                            break;
                                        case SpecialFunction.GCD_ID: GCD(specFunPos);
                                            break;
                                        case SpecialFunction.LCM_ID: LCM(specFunPos);
                                            break;

                                    }

                                } else
                                    /* ... 3-args functions  ... */
                                    if (f3ArgPos >= 0) {


                                        switch (tokensList[f3ArgPos].tokenId) {

                                            case Function3Arg.IF_ID: IF(f3ArgPos);
                                                break;
                                            case Function3Arg.CHI_ab_ID: CHI_ab(f3ArgPos);
                                                break;
                                            case Function3Arg.CHI_AB_ID: CHI_AB(f3ArgPos);
                                                break;
                                            case Function3Arg.CHI_Ab_ID: CHI_Ab(f3ArgPos);
                                                break;
                                            case Function3Arg.CHI_aB_ID: CHI_aB(f3ArgPos);
                                                break;

                                        }

                                    } else
                                        /* ... 2-args functions  ... */
                                        if (f2ArgPos >= 0) {

                                            switch (tokensList[f2ArgPos].tokenId) {

                                                case Function2Arg.LOG_ID: LOG(f2ArgPos);
                                                    break;
                                                case Function2Arg.MOD_ID: MOD(f2ArgPos);
                                                    break;
                                                case Function2Arg.BINOM_COEFF_ID: BINOM_COEFF(f2ArgPos);
                                                    break;
                                                case Function2Arg.BERNOULLI_NUMBER_ID: BERNOULLI_NUMBER(f2ArgPos);
                                                    break;
                                                case Function2Arg.STIRLING1_NUMBER_ID: STIRLING1_NUMBER(f2ArgPos);
                                                    break;
                                                case Function2Arg.STIRLING2_NUMBER_ID: STIRLING2_NUMBER(f2ArgPos);
                                                    break;
                                                case Function2Arg.WORPITZKY_NUMBER_ID: WORPITZKY_NUMBER(f2ArgPos);
                                                    break;
                                                case Function2Arg.EULER_NUMBER_ID: EULER_NUMBER(f2ArgPos);
                                                    break;
                                                case Function2Arg.KRONECKER_DELTA_ID: KRONECKER_DELTA(f2ArgPos);
                                                    break;
                                                case Function2Arg.EULER_POLYNOMIAL_ID: EULER_POLYNOMIAL(f2ArgPos);
                                                    break;
                                                case Function2Arg.HARMONIC_NUMBER_ID: HARMONIC2_NUMBER(f2ArgPos);
                                                    break;

                                            }

                                        } else
                                            /* ... 1-arg functions  ... */
                                            if (f1ArgPos >= 0) {

                                                switch (tokensList[f1ArgPos].tokenId) {

                                                    case Function1Arg.SIN_ID: SIN(f1ArgPos);
                                                        break;
                                                    case Function1Arg.COS_ID: COS(f1ArgPos);
                                                        break;
                                                    case Function1Arg.TAN_ID: TAN(f1ArgPos);
                                                        break;
                                                    case Function1Arg.CTAN_ID: CTAN(f1ArgPos);
                                                        break;
                                                    case Function1Arg.SEC_ID: SEC(f1ArgPos);
                                                        break;
                                                    case Function1Arg.COSEC_ID: COSEC(f1ArgPos);
                                                        break;
                                                    case Function1Arg.ASIN_ID: ASIN(f1ArgPos);
                                                        break;
                                                    case Function1Arg.ACOS_ID: ACOS(f1ArgPos);
                                                        break;
                                                    case Function1Arg.ATAN_ID: ATAN(f1ArgPos);
                                                        break;
                                                    case Function1Arg.ACTAN_ID: ACTAN(f1ArgPos);
                                                        break;
                                                    case Function1Arg.LN_ID: LN(f1ArgPos);
                                                        break;
                                                    case Function1Arg.LOG2_ID: LOG2(f1ArgPos);
                                                        break;
                                                    case Function1Arg.LOG10_ID: LOG10(f1ArgPos);
                                                        break;
                                                    case Function1Arg.RAD_ID: RAD(f1ArgPos);
                                                        break;
                                                    case Function1Arg.EXP_ID: EXP(f1ArgPos);
                                                        break;
                                                    case Function1Arg.SQRT_ID: SQRT(f1ArgPos);
                                                        break;
                                                    case Function1Arg.SINH_ID: SINH(f1ArgPos);
                                                        break;
                                                    case Function1Arg.COSH_ID: COSH(f1ArgPos);
                                                        break;
                                                    case Function1Arg.TANH_ID: TANH(f1ArgPos);
                                                        break;
                                                    case Function1Arg.COTH_ID: COTH(f1ArgPos);
                                                        break;
                                                    case Function1Arg.SECH_ID: SECH(f1ArgPos);
                                                        break;
                                                    case Function1Arg.CSCH_ID: CSCH(f1ArgPos);
                                                        break;
                                                    case Function1Arg.DEG_ID: DEG(f1ArgPos);
                                                        break;
                                                    case Function1Arg.ABS_ID: ABS(f1ArgPos);
                                                        break;
                                                    case Function1Arg.SGN_ID: SGN(f1ArgPos);
                                                        break;
                                                    case Function1Arg.FLOOR_ID: FLOOR(f1ArgPos);
                                                        break;
                                                    case Function1Arg.CEIL_ID: CEIL(f1ArgPos);
                                                        break;
                                                    case Function1Arg.NOT_ID: NOT(f1ArgPos);
                                                        break;
                                                    case Function1Arg.ARSINH_ID: ARSINH(f1ArgPos);
                                                        break;
                                                    case Function1Arg.ARCOSH_ID: ARCOSH(f1ArgPos);
                                                        break;
                                                    case Function1Arg.ARTANH_ID: ARTANH(f1ArgPos);
                                                        break;
                                                    case Function1Arg.ARCOTH_ID: ARCOTH(f1ArgPos);
                                                        break;
                                                    case Function1Arg.ARSECH_ID: ARSECH(f1ArgPos);
                                                        break;
                                                    case Function1Arg.ARCSCH_ID: ARCSCH(f1ArgPos);
                                                        break;
                                                    case Function1Arg.SA_ID: SA(f1ArgPos);
                                                        break;
                                                    case Function1Arg.SINC_ID: SINC(f1ArgPos);
                                                        break;
                                                    case Function1Arg.BELL_NUMBER_ID: BELL_NUMBER(f1ArgPos);
                                                        break;
                                                    case Function1Arg.LUCAS_NUMBER_ID: LUCAS_NUMBER(f1ArgPos);
                                                        break;
                                                    case Function1Arg.FIBONACCI_NUMBER_ID: FIBONACCI_NUMBER(f1ArgPos);
                                                        break;
                                                    case Function1Arg.HARMONIC_NUMBER_ID: HARMONIC_NUMBER(f1ArgPos);
                                                        break;
                                                }

                                            } else
                                                /* ... user functions  ... */
                                                if (userFunPos >= 0) {

                                                    USER_FUNCTION(userFunPos);

                                                } else
                                                    /* ... powering  ... */
                                                    if (powerPos >= 0) {
                                                        POWER(powerPos);
                                                    } else
                                                        if (factPos >= 0) {
                                                            FACT(factPos);
                                                        } else
                                                            if (modPos >= 0) {
                                                                MODULO(modPos);
                                                            } else
                                                                if (negPos >= 0) {
                                                                    NEG(negPos);
                                                                } else
                                                                    /* ... arithmetical operators  ... */
                                                                    if ((multiplyPos >= 0) || (dividePos >= 0)) {
                                                                        if ((multiplyPos >= 0) && (dividePos >= 0))
                                                                            if (multiplyPos <= dividePos)
                                                                                MULTIPLY(multiplyPos);
                                                                            else
                                                                                DIVIDE(dividePos);
                                                                        else
                                                                            if (multiplyPos >= 0)
                                                                                MULTIPLY(multiplyPos);
                                                                            else
                                                                                DIVIDE(dividePos);
                                                                    } else
                                                                        if ((minusPos >= 0) || (plusPos >= 0)) {

                                                                                if ((minusPos >= 0) && (plusPos >= 0))

                                                                                    if (minusPos <= plusPos)
                                                                                        MINUS(minusPos);
                                                                                    else
                                                                                        PLUS(plusPos);

                                                                                else
                                                                                    if (minusPos >= 0)
                                                                                        MINUS(minusPos);
                                                                                    else
                                                                                        PLUS(plusPos);



                                                                            } else
                                                                                if (neqPos >= 0) {
                                                                                    NEQ(neqPos);
                                                                                } else
                                                                                    /* ... binary relations ... */
                                                                                    if (eqPos >= 0) {
                                                                                        EQ(eqPos);
                                                                                    } else
                                                                                        if (ltPos >= 0) {
                                                                                            LT(ltPos);
                                                                                        } else
                                                                                            if (gtPos >= 0) {
                                                                                                GT(gtPos);
                                                                                            } else
                                                                                                if (leqPos >= 0) {
                                                                                                    LEQ(leqPos);
                                                                                                } else
                                                                                                    if (geqPos >= 0) {
                                                                                                        GEQ(geqPos);
                                                                                                    } else
                                                                                                        if (commaPos >= 0) {


                                                                                                            for (int i = commas.Count - 1; i >= 0; i--)
                                                                                                                COMMA(commas[i]);

                                                                                                        } else
                                                                                                            /* ... logical operators  ... */
                                                                                                            if (bolPos >= 0) {

                                                                                                                switch (tokensList[bolPos].tokenId) {

                                                                                                                    case BooleanOperator.AND_ID: AND(bolPos);
                                                                                                                        break;
                                                                                                                    case BooleanOperator.CIMP_ID: CIMP(bolPos);
                                                                                                                        break;
                                                                                                                    case BooleanOperator.CNIMP_ID: CNIMP(bolPos);
                                                                                                                        break;
                                                                                                                    case BooleanOperator.EQV_ID: EQV(bolPos);
                                                                                                                        break;
                                                                                                                    case BooleanOperator.IMP_ID: IMP(bolPos);
                                                                                                                        break;
                                                                                                                    case BooleanOperator.NAND_ID: NAND(bolPos);
                                                                                                                        break;
                                                                                                                    case BooleanOperator.NIMP_ID: NIMP(bolPos);
                                                                                                                        break;
                                                                                                                    case BooleanOperator.NOR_ID: NOR(bolPos);
                                                                                                                        break;
                                                                                                                    case BooleanOperator.OR_ID: OR(bolPos);
                                                                                                                        break;
                                                                                                                    case BooleanOperator.XOR_ID: XOR(bolPos);
                                                                                                                        break;


                                                                                                                }

                                                                                                            } else
                                                                                                                if ((lParPos >= 0) && (rParPos > lParPos)) {
                                                                                                                    PARENTHESES(lParPos, rParPos);
                                                                                                                }

                if (verboseMode == true) {
                    showParsing(0, tokensList.Count - 1);
                    printSystemInfo(" done\n", NO_EXP_STR);
                }

                //wait(1);

            } while (tokensList.Count > 1);


            if (verboseMode == true) {
                //printSystemInfo("\n", WITH_EXP_STR);
                printSystemInfo("Calculated value: " + tokensList[0].tokenValue + "\n", WITH_EXP_STR);
                printSystemInfo("Exiting\n", WITH_EXP_STR);
                printSystemInfo("\n", NO_EXP_STR);
            }

            long endTime = mXparser.currentTimeMillis();

            computingTime = (endTime - startTime) / 1000.0;


            return tokensList[0].tokenValue;
        }
    	
    	
	    /*=================================================
	     * 
	     * Parser methods 
	     * 
	     *================================================= 
	     */
    	
    	
	    /**
	     * Creates parseres key words list
	     */
	    private void addParserKeyWords() {
    		
    		
		    /*
		     * Operators key words
		     */		
		    addKeyWord(Operator.PLUS_STR, Operator.PLUS_DESC, Operator.PLUS_ID, Operator.TYPE_ID);
		    addKeyWord(Operator.MINUS_STR, Operator.MINUS_DESC, Operator.MINUS_ID, Operator.TYPE_ID);
		    addKeyWord(Operator.MULTIPLY_STR, Operator.MULTIPLY_DESC, Operator.MULTIPLY_ID, Operator.TYPE_ID);
		    addKeyWord(Operator.DIVIDE_STR, Operator.DIVIDE_DESC, Operator.DIVIDE_ID, Operator.TYPE_ID);
		    addKeyWord(Operator.POWER_STR, Operator.POWER_DESC, Operator.POWER_ID, Operator.TYPE_ID);
		    addKeyWord(Operator.FACT_STR, Operator.FACT_DESC, Operator.FACT_ID, Operator.TYPE_ID);
		    addKeyWord(Operator.MOD_STR, Operator.MOD_DESC, Operator.MOD_ID, Operator.TYPE_ID);

		    /*
		     * bool operators key words
		     */				
		    addKeyWord(BooleanOperator.AND_STR, BooleanOperator.AND_DESC, BooleanOperator.AND_ID, BooleanOperator.TYPE_ID);
		    addKeyWord(BooleanOperator.AND1_STR, BooleanOperator.AND_DESC, BooleanOperator.AND_ID, BooleanOperator.TYPE_ID);
		    addKeyWord(BooleanOperator.AND2_STR, BooleanOperator.AND_DESC, BooleanOperator.AND_ID, BooleanOperator.TYPE_ID);
		    addKeyWord(BooleanOperator.NAND_STR, BooleanOperator.NAND_DESC, BooleanOperator.NAND_ID, BooleanOperator.TYPE_ID);
		    addKeyWord(BooleanOperator.NAND1_STR, BooleanOperator.NAND_DESC, BooleanOperator.NAND_ID, BooleanOperator.TYPE_ID);
		    addKeyWord(BooleanOperator.NAND2_STR, BooleanOperator.NAND_DESC, BooleanOperator.NAND_ID, BooleanOperator.TYPE_ID);
		    addKeyWord(BooleanOperator.OR_STR, BooleanOperator.OR_DESC, BooleanOperator.OR_ID, BooleanOperator.TYPE_ID);
		    addKeyWord(BooleanOperator.OR1_STR, BooleanOperator.OR_DESC, BooleanOperator.OR_ID, BooleanOperator.TYPE_ID);
		    addKeyWord(BooleanOperator.OR2_STR, BooleanOperator.OR_DESC, BooleanOperator.OR_ID, BooleanOperator.TYPE_ID);
		    addKeyWord(BooleanOperator.NOR_STR, BooleanOperator.NOR_DESC, BooleanOperator.NOR_ID, BooleanOperator.TYPE_ID);
		    addKeyWord(BooleanOperator.NOR1_STR, BooleanOperator.NOR_DESC, BooleanOperator.NOR_ID, BooleanOperator.TYPE_ID);
		    addKeyWord(BooleanOperator.NOR2_STR, BooleanOperator.NOR_DESC, BooleanOperator.NOR_ID, BooleanOperator.TYPE_ID);
		    addKeyWord(BooleanOperator.XOR_STR, BooleanOperator.XOR_DESC, BooleanOperator.XOR_ID, BooleanOperator.TYPE_ID);
		    addKeyWord(BooleanOperator.IMP_STR, BooleanOperator.IMP_DESC, BooleanOperator.IMP_ID, BooleanOperator.TYPE_ID);
		    addKeyWord(BooleanOperator.NIMP_STR, BooleanOperator.NIMP_DESC, BooleanOperator.NIMP_ID, BooleanOperator.TYPE_ID);
		    addKeyWord(BooleanOperator.CIMP_STR, BooleanOperator.CIMP_DESC, BooleanOperator.CIMP_ID, BooleanOperator.TYPE_ID);
		    addKeyWord(BooleanOperator.CNIMP_STR, BooleanOperator.CNIMP_DESC, BooleanOperator.CNIMP_ID, BooleanOperator.TYPE_ID);
		    addKeyWord(BooleanOperator.EQV_STR, BooleanOperator.EQV_DESC, BooleanOperator.EQV_ID, BooleanOperator.TYPE_ID);
		    addKeyWord(BooleanOperator.NEG_STR, BooleanOperator.NEG_DESC, BooleanOperator.NEG_ID, BooleanOperator.TYPE_ID);
    		
		    /*
		     * Binary relations key words
		     */		
		    addKeyWord(BinaryRelation.EQ_STR, BinaryRelation.EQ_DESC, BinaryRelation.EQ_ID, BinaryRelation.TYPE_ID);
		    addKeyWord(BinaryRelation.EQ1_STR, BinaryRelation.EQ_DESC, BinaryRelation.EQ_ID, BinaryRelation.TYPE_ID);
		    addKeyWord(BinaryRelation.NEQ_STR, BinaryRelation.NEQ_DESC, BinaryRelation.NEQ_ID, BinaryRelation.TYPE_ID);
		    addKeyWord(BinaryRelation.NEQ1_STR, BinaryRelation.NEQ_DESC, BinaryRelation.NEQ_ID, BinaryRelation.TYPE_ID);
		    addKeyWord(BinaryRelation.NEQ2_STR, BinaryRelation.NEQ_DESC, BinaryRelation.NEQ_ID, BinaryRelation.TYPE_ID);
		    addKeyWord(BinaryRelation.LT_STR, BinaryRelation.LT_DESC, BinaryRelation.LT_ID, BinaryRelation.TYPE_ID);
		    addKeyWord(BinaryRelation.GT_STR, BinaryRelation.GT_DESC, BinaryRelation.GT_ID, BinaryRelation.TYPE_ID);
		    addKeyWord(BinaryRelation.LEQ_STR, BinaryRelation.LEQ_DESC, BinaryRelation.LEQ_ID, BinaryRelation.TYPE_ID);
		    addKeyWord(BinaryRelation.GEQ_STR, BinaryRelation.GEQ_DESC, BinaryRelation.GEQ_ID, BinaryRelation.TYPE_ID);

		    /*
		     * 1 arg functions key words
		     */
		    addKeyWord(Function1Arg.SIN_STR, Function1Arg.SIN_DESC, Function1Arg.SIN_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.COS_STR, Function1Arg.COS_DESC, Function1Arg.COS_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.TAN_STR, Function1Arg.TAN_DESC, Function1Arg.TAN_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.TG_STR, Function1Arg.TAN_DESC, Function1Arg.TAN_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.CTAN_STR, Function1Arg.CTAN_DESC, Function1Arg.CTAN_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.CTG_STR, Function1Arg.CTAN_DESC, Function1Arg.CTAN_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.COT_STR, Function1Arg.CTAN_DESC, Function1Arg.CTAN_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.SEC_STR, Function1Arg.SEC_DESC, Function1Arg.SEC_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.COSEC_STR, Function1Arg.COSEC_DESC, Function1Arg.COSEC_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.CSC_STR, Function1Arg.COSEC_DESC, Function1Arg.COSEC_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ASIN_STR, Function1Arg.ASIN_DESC, Function1Arg.ASIN_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ARSIN_STR, Function1Arg.ASIN_DESC, Function1Arg.ASIN_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ARCSIN_STR, Function1Arg.ASIN_DESC, Function1Arg.ASIN_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ACOS_STR, Function1Arg.ACOS_DESC, Function1Arg.ACOS_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ARCOS_STR, Function1Arg.ACOS_DESC, Function1Arg.ACOS_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ARCCOS_STR, Function1Arg.ACOS_DESC, Function1Arg.ACOS_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ATAN_STR, Function1Arg.ATAN_DESC, Function1Arg.ATAN_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ARCTAN_STR, Function1Arg.ATAN_DESC, Function1Arg.ATAN_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ATG_STR, Function1Arg.ATAN_DESC, Function1Arg.ATAN_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ARCTG_STR, Function1Arg.ATAN_DESC, Function1Arg.ATAN_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ACTAN_STR, Function1Arg.ACTAN_DESC, Function1Arg.ACTAN_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ARCCTAN_STR, Function1Arg.ACTAN_DESC, Function1Arg.ACTAN_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ACTG_STR, Function1Arg.ACTAN_DESC, Function1Arg.ACTAN_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ARCCTG_STR, Function1Arg.ACTAN_DESC, Function1Arg.ACTAN_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ACOT_STR, Function1Arg.ACTAN_DESC, Function1Arg.ACTAN_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ARCCOT_STR, Function1Arg.ACTAN_DESC, Function1Arg.ACTAN_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.LN_STR, Function1Arg.LN_DESC, Function1Arg.LN_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.LOG2_STR, Function1Arg.LOG2_DESC, Function1Arg.LOG2_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.LOG10_STR, Function1Arg.LOG10_DESC, Function1Arg.LOG10_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.RAD_STR, Function1Arg.RAD_DESC, Function1Arg.RAD_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.EXP_STR, Function1Arg.EXP_DESC, Function1Arg.EXP_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.SQRT_STR, Function1Arg.SQRT_DESC, Function1Arg.SQRT_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.SINH_STR, Function1Arg.SINH_DESC, Function1Arg.SINH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.COSH_STR, Function1Arg.COSH_DESC, Function1Arg.COSH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.TANH_STR, Function1Arg.TANH_DESC, Function1Arg.TANH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.TGH_STR, Function1Arg.TANH_DESC, Function1Arg.TANH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.CTANH_STR, Function1Arg.COTH_DESC, Function1Arg.COTH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.COTH_STR, Function1Arg.COTH_DESC, Function1Arg.COTH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.CTGH_STR, Function1Arg.COTH_DESC, Function1Arg.COTH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.SECH_STR, Function1Arg.SECH_DESC, Function1Arg.SECH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.CSCH_STR, Function1Arg.CSCH_DESC, Function1Arg.CSCH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.COSECH_STR, Function1Arg.CSCH_DESC, Function1Arg.CSCH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.DEG_STR, Function1Arg.DEG_DESC, Function1Arg.DEG_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ABS_STR, Function1Arg.ABS_DESC, Function1Arg.ABS_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.SGN_STR, Function1Arg.SGN_DESC, Function1Arg.SGN_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.FLOOR_STR, Function1Arg.FLOOR_DESC, Function1Arg.FLOOR_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.CEIL_STR, Function1Arg.CEIL_DESC, Function1Arg.CEIL_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.NOT_STR, Function1Arg.NOT_DESC, Function1Arg.NOT_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ASINH_STR, Function1Arg.ARSINH_DESC, Function1Arg.ARSINH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ARSINH_STR, Function1Arg.ARSINH_DESC, Function1Arg.ARSINH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ARCSINH_STR, Function1Arg.ARSINH_DESC, Function1Arg.ARSINH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ACOSH_STR, Function1Arg.ARCOSH_DESC, Function1Arg.ARCOSH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ARCOSH_STR, Function1Arg.ARCOSH_DESC, Function1Arg.ARCOSH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ARCCOSH_STR, Function1Arg.ARCOSH_DESC, Function1Arg.ARCOSH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ATANH_STR, Function1Arg.ARTANH_DESC, Function1Arg.ARTANH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ARCTANH_STR, Function1Arg.ARTANH_DESC, Function1Arg.ARTANH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ATGH_STR, Function1Arg.ARTANH_DESC, Function1Arg.ARTANH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ARCTGH_STR, Function1Arg.ARTANH_DESC, Function1Arg.ARTANH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ACTANH_STR, Function1Arg.ARCOTH_DESC, Function1Arg.ARCOTH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ARCCTANH_STR, Function1Arg.ARCOTH_DESC, Function1Arg.ARCOTH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ACOTH_STR, Function1Arg.ARCOTH_DESC, Function1Arg.ARCOTH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ARCOTH_STR, Function1Arg.ARCOTH_DESC, Function1Arg.ARCOTH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ARCCOTH_STR, Function1Arg.ARCOTH_DESC, Function1Arg.ARCOTH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ACTGH_STR, Function1Arg.ARCOTH_DESC, Function1Arg.ARCOTH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ARCCTGH_STR, Function1Arg.ARCOTH_DESC, Function1Arg.ARCOTH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ASECH_STR, Function1Arg.ARSECH_DESC, Function1Arg.ARSECH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ARSECH_STR, Function1Arg.ARSECH_DESC, Function1Arg.ARSECH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ARCSECH_STR, Function1Arg.ARSECH_DESC, Function1Arg.ARSECH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ACSCH_STR, Function1Arg.ARCSCH_DESC, Function1Arg.ARCSCH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ARCSCH_STR, Function1Arg.ARCSCH_DESC, Function1Arg.ARCSCH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ARCCSCH_STR, Function1Arg.ARCSCH_DESC, Function1Arg.ARCSCH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ACOSECH_STR, Function1Arg.ARCSCH_DESC, Function1Arg.ARCSCH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ARCOSECH_STR, Function1Arg.ARCSCH_DESC, Function1Arg.ARCSCH_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.ARCCOSECH_STR, Function1Arg.ARCSCH_DESC, Function1Arg.ARCSCH_ID, Function1Arg.TYPE_ID);
    		
		    addKeyWord(Function1Arg.SA_STR, Function1Arg.SA_DESC, Function1Arg.SA_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.SA1_STR, Function1Arg.SA_DESC, Function1Arg.SA_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.SINC_STR, Function1Arg.SINC_DESC, Function1Arg.SINC_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.BELL_NUMBER_STR, Function1Arg.BELL_NUMBER_DESC, Function1Arg.BELL_NUMBER_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.FIBONACCI_NUMBER_STR, Function1Arg.FIBONACCI_NUMBER_DESC, Function1Arg.FIBONACCI_NUMBER_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.LUCAS_NUMBER_STR, Function1Arg.LUCAS_NUMBER_DESC, Function1Arg.LUCAS_NUMBER_ID, Function1Arg.TYPE_ID);
		    addKeyWord(Function1Arg.HARMONIC_NUMBER_STR, Function1Arg.HARMONIC_NUMBER_DESC, Function1Arg.HARMONIC_NUMBER_ID, Function1Arg.TYPE_ID);

		    /*
		     * 2 args functions key words
		     */		
		    addKeyWord(Function2Arg.LOG_STR, Function2Arg.LOG_DESC, Function2Arg.LOG_ID, Function2Arg.TYPE_ID);
		    addKeyWord(Function2Arg.MOD_STR, Function2Arg.MOD_DESC, Function2Arg.MOD_ID, Function2Arg.TYPE_ID);
		    addKeyWord(Function2Arg.BINOM_COEFF_STR, Function2Arg.BINOM_COEFF_DESC, Function2Arg.BINOM_COEFF_ID, Function2Arg.TYPE_ID);
		    addKeyWord(Function2Arg.BERNOULLI_NUMBER_STR, Function2Arg.BERNOULLI_NUMBER_DESC, Function2Arg.BERNOULLI_NUMBER_ID, Function2Arg.TYPE_ID);
		    addKeyWord(Function2Arg.STIRLING1_NUMBER_STR, Function2Arg.STIRLING1_NUMBER_DESC, Function2Arg.STIRLING1_NUMBER_ID, Function2Arg.TYPE_ID);
		    addKeyWord(Function2Arg.STIRLING2_NUMBER_STR, Function2Arg.STIRLING2_NUMBER_DESC, Function2Arg.STIRLING2_NUMBER_ID, Function2Arg.TYPE_ID);
		    addKeyWord(Function2Arg.WORPITZKY_NUMBER_STR, Function2Arg.WORPITZKY_NUMBER_DESC, Function2Arg.WORPITZKY_NUMBER_ID, Function2Arg.TYPE_ID);
		    addKeyWord(Function2Arg.EULER_NUMBER_STR, Function2Arg.EULER_NUMBER_DESC, Function2Arg.EULER_NUMBER_ID, Function2Arg.TYPE_ID);
		    addKeyWord(Function2Arg.KRONECKER_DELTA_STR, Function2Arg.KRONECKER_DELTA_DESC, Function2Arg.KRONECKER_DELTA_ID, Function2Arg.TYPE_ID);
		    addKeyWord(Function2Arg.EULER_POLYNOMIAL_STR, Function2Arg.EULER_POLYNOMIAL_DESC, Function2Arg.EULER_POLYNOMIAL_ID, Function2Arg.TYPE_ID);
		    addKeyWord(Function2Arg.HARMONIC_NUMBER_STR, Function2Arg.HARMONIC_NUMBER_DESC, Function2Arg.HARMONIC_NUMBER_ID, Function2Arg.TYPE_ID);
    		
		    /*
		     * 3 args functions key words
		     */		
		    addKeyWord(Function3Arg.IF_STR, Function3Arg.IF_DESC, Function3Arg.IF_CONDITION_ID, Function3Arg.TYPE_ID);
		    addKeyWord(Function3Arg.CHI_ab_STR, Function3Arg.CHI_ab_DESC, Function3Arg.CHI_ab_ID, Function3Arg.TYPE_ID);
		    addKeyWord(Function3Arg.CHI_AB_STR, Function3Arg.CHI_AB_DESC, Function3Arg.CHI_AB_ID, Function3Arg.TYPE_ID);
		    addKeyWord(Function3Arg.CHI_Ab_STR, Function3Arg.CHI_Ab_DESC, Function3Arg.CHI_Ab_ID, Function3Arg.TYPE_ID);
		    addKeyWord(Function3Arg.CHI_aB_STR, Function3Arg.CHI_aB_DESC, Function3Arg.CHI_aB_ID, Function3Arg.TYPE_ID);
    		
		    /*
		     * Speciall functions as key words
		     */
		    addKeyWord(SpecialFunction.IFF_STR, SpecialFunction.IFF_DESC, SpecialFunction.IFF_ID, SpecialFunction.TYPE_ID);
		    addKeyWord(SpecialFunction.MIN_STR, SpecialFunction.MIN_DESC, SpecialFunction.MIN_ID, SpecialFunction.TYPE_ID);
		    addKeyWord(SpecialFunction.MAX_STR, SpecialFunction.MAX_DESC, SpecialFunction.MAX_ID, SpecialFunction.TYPE_ID);
		    addKeyWord(SpecialFunction.CONT_FRAC_STR, SpecialFunction.CONT_FRAC_DESC, SpecialFunction.CONT_FRAC_ID, SpecialFunction.TYPE_ID);
		    addKeyWord(SpecialFunction.CONT_POL_STR, SpecialFunction.CONT_POL_DESC, SpecialFunction.CONT_POL_ID, SpecialFunction.TYPE_ID);
		    addKeyWord(SpecialFunction.GCD_STR, SpecialFunction.GCD_DESC, SpecialFunction.GCD_ID, SpecialFunction.TYPE_ID);
		    addKeyWord(SpecialFunction.LCM_STR, SpecialFunction.LCM_DESC, SpecialFunction.LCM_ID, SpecialFunction.TYPE_ID);
    		
		    /*
		     * Calculus key words
		     */
		    addKeyWord(Calculus.SUM_STR, Calculus.SUM_DESC, Calculus.SUM_ID, Calculus.TYPE_ID);
		    addKeyWord(Calculus.PROD_STR, Calculus.PROD_DESC, Calculus.PROD_ID, Calculus.TYPE_ID);
		    addKeyWord(Calculus.INT_STR, Calculus.INT_DESC, Calculus.INT_ID, Calculus.TYPE_ID);
		    addKeyWord(Calculus.DER_STR, Calculus.DER_DESC, Calculus.DER_ID, Calculus.TYPE_ID);
		    addKeyWord(Calculus.DER_LEFT_STR, Calculus.DER_LEFT_DESC, Calculus.DER_LEFT_ID, Calculus.TYPE_ID);
		    addKeyWord(Calculus.DER_RIGHT_STR, Calculus.DER_RIGHT_DESC, Calculus.DER_RIGHT_ID, Calculus.TYPE_ID);
		    addKeyWord(Calculus.DERN_STR, Calculus.DERN_DESC, Calculus.DERN_ID, Calculus.TYPE_ID);
		    addKeyWord(Calculus.FORW_DIFF_STR, Calculus.FORW_DIFF_DESC, Calculus.FORW_DIFF_ID, Calculus.TYPE_ID);
		    addKeyWord(Calculus.BACKW_DIFF_STR, Calculus.BACKW_DIFF_DESC, Calculus.BACKW_DIFF_ID, Calculus.TYPE_ID);

    		
    		
		    /*
		     * Constants key words
		     */
		    addKeyWord(Const.PI_STR, Const.PI_DESC, Const.PI_ID, Const.TYPE_ID);
		    addKeyWord(Const.EULER_STR, Const.EULER_DESC, Const.EULER_ID, Const.TYPE_ID);
		    addKeyWord(Const.EULER_MASCHERONI_STR, Const.EULER_MASCHERONI_DESC, Const.EULER_MASCHERONI_ID, Const.TYPE_ID);
		    addKeyWord(Const.GOLDEN_RATIO_STR, Const.GOLDEN_RATIO_DESC, Const.GOLDEN_RATIO_ID, Const.TYPE_ID);
		    addKeyWord(Const.PLASTIC_STR, Const.PLASTIC_DESC, Const.PLASTIC_ID, Const.TYPE_ID);
		    addKeyWord(Const.EMBREE_TREFETHEN_STR, Const.EMBREE_TREFETHEN_DESC, Const.EMBREE_TREFETHEN_ID, Const.TYPE_ID);
		    addKeyWord(Const.FEIGENBAUM_DELTA_STR, Const.FEIGENBAUM_DELTA_DESC, Const.FEIGENBAUM_DELTA_ID, Const.TYPE_ID);
		    addKeyWord(Const.FEIGENBAUM_ALFA_STR, Const.FEIGENBAUM_ALFA_DESC, Const.FEIGENBAUM_ALFA_ID, Const.TYPE_ID);
		    addKeyWord(Const.TWIN_PRIME_STR, Const.TWIN_PRIME_DESC, Const.TWIN_PRIME_ID, Const.TYPE_ID);
		    addKeyWord(Const.MEISSEL_MERTEENS_STR, Const.MEISSEL_MERTEENS_DESC, Const.MEISSEL_MERTEENS_ID, Const.TYPE_ID);
		    addKeyWord(Const.BRAUN_TWIN_PRIME_STR, Const.BRAUN_TWIN_PRIME_DESC, Const.BRAUN_TWIN_PRIME_ID, Const.TYPE_ID);
		    addKeyWord(Const.BRAUN_PRIME_QUADR_STR, Const.BRAUN_PRIME_QUADR_DESC, Const.BRAUN_PRIME_QUADR_ID, Const.TYPE_ID);
		    addKeyWord(Const.BRUIJN_NEWMAN_STR, Const.BRUIJN_NEWMAN_DESC, Const.BRUIJN_NEWMAN_ID, Const.TYPE_ID);
		    addKeyWord(Const.CATALAN_STR, Const.CATALAN_DESC, Const.CATALAN_ID, Const.TYPE_ID);
		    addKeyWord(Const.LANDAU_RAMANUJAN_STR, Const.LANDAU_RAMANUJAN_DESC, Const.LANDAU_RAMANUJAN_ID, Const.TYPE_ID);
		    addKeyWord(Const.VISWANATH_STR, Const.VISWANATH_DESC, Const.VISWANATH_ID, Const.TYPE_ID);
		    addKeyWord(Const.LEGENDRE_STR, Const.LEGENDRE_DESC, Const.LEGENDRE_ID, Const.TYPE_ID);
		    addKeyWord(Const.RAMANUJAN_SOLDNER_STR, Const.RAMANUJAN_SOLDNER_DESC, Const.RAMANUJAN_SOLDNER_ID, Const.TYPE_ID);
		    addKeyWord(Const.ERDOS_BORWEIN_STR, Const.ERDOS_BORWEIN_DESC, Const.ERDOS_BORWEIN_ID, Const.TYPE_ID);
		    addKeyWord(Const.BERNSTEIN_STR, Const.BERNSTEIN_DESC, Const.BERNSTEIN_ID, Const.TYPE_ID);
		    addKeyWord(Const.GAUSS_KUZMIN_WIRSING_STR, Const.GAUSS_KUZMIN_WIRSING_DESC, Const.GAUSS_KUZMIN_WIRSING_ID, Const.TYPE_ID);
		    addKeyWord(Const.HAFNER_SARNAK_MCCURLEY_STR, Const.HAFNER_SARNAK_MCCURLEY_DESC, Const.HAFNER_SARNAK_MCCURLEY_ID, Const.TYPE_ID);
		    addKeyWord(Const.GOLOMB_DICKMAN_STR, Const.GOLOMB_DICKMAN_DESC, Const.GOLOMB_DICKMAN_ID, Const.TYPE_ID);
		    addKeyWord(Const.CAHEN_STR, Const.CAHEN_DESC, Const.CAHEN_ID, Const.TYPE_ID);
		    addKeyWord(Const.LAPLACE_LIMIT_STR, Const.LAPLACE_LIMIT_DESC, Const.LAPLACE_LIMIT_ID, Const.TYPE_ID);
		    addKeyWord(Const.ALLADI_GRINSTEAD_STR, Const.ALLADI_GRINSTEAD_DESC, Const.ALLADI_GRINSTEAD_ID, Const.TYPE_ID);
		    addKeyWord(Const.LENGYEL_STR, Const.LENGYEL_DESC, Const.LENGYEL_ID, Const.TYPE_ID);
		    addKeyWord(Const.LEVY_STR, Const.LEVY_DESC, Const.LEVY_ID, Const.TYPE_ID);
		    addKeyWord(Const.APERY_STR, Const.APERY_DESC, Const.APERY_ID, Const.TYPE_ID);
		    addKeyWord(Const.MILLS_STR, Const.MILLS_DESC, Const.MILLS_ID, Const.TYPE_ID);
		    addKeyWord(Const.BACKHOUSE_STR, Const.BACKHOUSE_DESC, Const.BACKHOUSE_ID, Const.TYPE_ID);
		    addKeyWord(Const.PORTER_STR, Const.PORTER_DESC, Const.PORTER_ID, Const.TYPE_ID);
		    addKeyWord(Const.LIEB_QUARE_ICE_STR, Const.LIEB_QUARE_ICE_DESC, Const.LIEB_QUARE_ICE_ID, Const.TYPE_ID);
		    addKeyWord(Const.NIVEN_STR, Const.NIVEN_DESC, Const.NIVEN_ID, Const.TYPE_ID);
		    addKeyWord(Const.SIERPINSKI_STR, Const.SIERPINSKI_DESC, Const.SIERPINSKI_ID, Const.TYPE_ID);
		    addKeyWord(Const.KHINCHIN_STR, Const.KHINCHIN_DESC, Const.KHINCHIN_ID, Const.TYPE_ID);
		    addKeyWord(Const.FRANSEN_ROBINSON_STR, Const.FRANSEN_ROBINSON_DESC, Const.FRANSEN_ROBINSON_ID, Const.TYPE_ID);
		    addKeyWord(Const.LANDAU_STR, Const.LANDAU_DESC, Const.LANDAU_ID, Const.TYPE_ID);
		    addKeyWord(Const.PARABOLIC_STR, Const.PARABOLIC_DESC, Const.PARABOLIC_ID, Const.TYPE_ID);
		    addKeyWord(Const.OMEGA_STR, Const.OMEGA_DESC, Const.OMEGA_ID, Const.TYPE_ID);
		    addKeyWord(Const.MRB_STR, Const.MRB_DESC, Const.MRB_ID, Const.TYPE_ID);

    		
    		
		    /*
		     * Other parser symbols key words
		     */						
		    addKeyWord(ParserSymbol.LEFT_PARENTHESES_STR, ParserSymbol.LEFT_PARENTHESES_DESC, ParserSymbol.LEFT_PARENTHESES_ID, ParserSymbol.TYPE_ID);
		    addKeyWord(ParserSymbol.RIGHT_PARENTHESES_STR, ParserSymbol.RIGHT_PARENTHESES_DESC, ParserSymbol.RIGHT_PARENTHESES_ID, ParserSymbol.TYPE_ID);
		    addKeyWord(ParserSymbol.COMMA_STR, ParserSymbol.COMMA_DESC, ParserSymbol.COMMA_ID, ParserSymbol.TYPE_ID);
		    addKeyWord(ParserSymbol.SEMI_STR, ParserSymbol.SEMI_DESC, ParserSymbol.COMMA_ID, ParserSymbol.TYPE_ID);
		    addKeyWord(ParserSymbol.NUMBER_REG_EXP, ParserSymbol.NUMBER_REG_DESC, ParserSymbol.NUMBER_ID, ParserSymbol.NUMBER_TYPE_ID);
    		
    		
    		
	    }
    	
	    /**
	     * Adds arguments key words to the keywords list
	     */	
	    private void addArgumentsKeyWords() {

		    int argumentsNumber = argumentsList.Count;
		    for (int argumentIndex = 0; argumentIndex<argumentsNumber; argumentIndex++) {
			    Argument arg = argumentsList[argumentIndex];
    			
			    if (arg.getArgumentType() != Argument.RECURSIVE_ARGUMENT)
				    addKeyWord(arg.getArgumentName(),arg.getDescription(), argumentIndex,Argument.TYPE_ID);
			    else
				    addKeyWord(arg.getArgumentName(),arg.getDescription(), argumentIndex,RecursiveArgument.TYPE_ID);
    			
		    }

	    }
    	
	    /**
	     * Adds functions key words to the keywords list
	     */
	    private void addFunctionsKeyWords() {

		    int functionsNumber = functionsList.Count;
		    for (int functionIndex = 0; functionIndex<functionsNumber; functionIndex++) {
			    Function fun = functionsList[functionIndex];
			    addKeyWord(fun.getFunctionName(),fun.getDescription(), functionIndex,Function.TYPE_ID);		
		    }

	    }	
    	
	    /**
	     * Adds constants key words to the keywords list
	     */
	    private void addConstantsKeyWords() {

		    int constantsNumber = constantsList.Count;

		    for (int constantIndex = 0; constantIndex < constantsNumber; constantIndex++) {
			    Constant c = constantsList[constantIndex];
			    addKeyWord(c.getConstantName(), c.getDescription(), constantIndex, Constant.TYPE_ID);		
		    }

	    }		
    	
    		
	    private void addKeyWord(String wordString, String wordDescription, int wordId, int wordTypeId) {

		    keyWordsList.Add(new KeyWord(wordString, wordDescription, wordId, wordTypeId));

	    }	
	    /**
	     * Adds expression token
	     * Method is caleld by the tokenExpressionString()
	     * while parsing string expression
	     * 
	     * @param      tokenStr            the token string
	     * @param      keyWord             the key word
	     */	
	    private void addToken(String tokenStr, KeyWord keyWord) {
    		
		    Token token = new Token();
		    initialTokens.Add(token);

		    token.tokenStr = tokenStr;
		    token.keyWord = keyWord.wordString;
    		
		    token.tokenId = keyWord.wordId;
		    token.tokenTypeId = keyWord.wordTypeId;
    		
		    if (token.tokenTypeId == Argument.TYPE_ID)
			    token.tokenValue = argumentsList[token.tokenId].argumentValue;
		    else
			    if (token.tokenTypeId == ParserSymbol.NUMBER_TYPE_ID) {
                    token.tokenValue = Double.Parse(token.tokenStr, NumberStyles.Float, CultureInfo.InvariantCulture);
				    token.keyWord = ParserSymbol.NUMBER_STR;
			    }
    		
	    }

	    /**
	     * Tokenizing expressiong string
	     */		
	    private void tokenizeExpressionString() {
    		
    		
		    /*
		     * Add parser and argument key words
		     */
    		
		    keyWordsList = new List<KeyWord>();
    		
		    addParserKeyWords();
		    addArgumentsKeyWords();
		    addFunctionsKeyWords();
		    addConstantsKeyWords();
    			
		    keyWordsList.Sort(new DescKwLenComparator());
    				
		    /*
		     * Evaluate position after soritng for the following kwywords types
		     *    number
		     *    plus operator
		     *    minus operator
		     * 
		     * Above mentioned information is required 
		     * when distinguishing between numbers (regexp) and operators
		     * 
		     * For exmaple
		     * 
		     * 1-2 : two numbers and one operator, but -2 is also a valid number
		     * (-2)+3 : two number and one operator
		     */
		    int numberKwId = Const.NaN;
		    int plusKwId = Const.NaN;
		    int minusKwId = Const.NaN;
    		
		    for (int kwId = 0; kwId < keyWordsList.Count; kwId++) {
			    if ( keyWordsList[kwId].wordTypeId == ParserSymbol.NUMBER_TYPE_ID)
				    numberKwId = kwId;
    			
			    if ( keyWordsList[kwId].wordTypeId == Operator.TYPE_ID) {
    				
				    if (keyWordsList[kwId].wordId == Operator.PLUS_ID)
					    plusKwId = kwId;
				    if (keyWordsList[kwId].wordId == Operator.MINUS_ID)
					    minusKwId = kwId;
    				
			    }
    			
		    }
    		
		    /*
		     * Clearing expression string from spaces
		     */
		    String newExpressionString = "";
		    char c;
		    if (expressionString.Length > 0) {
			    for (int i = 0; i < expressionString.Length; i++) {
				    c = expressionString[i];
				    if ( (c != ' ') && (c != '\n') && (c != '\r') && (c != '\t') ) newExpressionString = newExpressionString + c;
			    }
		    }
    		
    		
    		
		    /*
		     * words list and tokens list
		     */
		    initialTokens = new List<Token>();
    			
		    int lastPos = 0; /* position of the key word previously added*/
		    int pos = 0; /* current position */
		    String tokenStr = "";
		    int matchStatusPrev = NOT_FOUND; /* unknown key word (previous) */
		    int matchStatus = NOT_FOUND; /* unknown key word (current) */
    		

		    /*
		     * Check all available positions in the expression tokens list
		     */
		    do {
    			
			    /*
			     * 1st step
			     * 
			     * compare with the regExp for real numbers
			     * find the longest word which could be matched
			     * with the given regExp
			     */
			    int numEnd = -1;
			    for (int i=pos; i<newExpressionString.Length; i++) {
  
				    String str = newExpressionString.Substring(pos, i+1-pos);


                    /*
                    if (    Regex.Match(str, ParserSymbol.NUMBER).Success )
                    {
					    numEnd = i;
                        mXparser.consolePrintln("Regex -> |" + str + "=number" + " " + pos + ", " + i);
				    }*/

                    try
                    {
                        Double.Parse(str, NumberStyles.Float, CultureInfo.InvariantCulture);
                        numEnd = i;
                    }
                    catch (Exception e)
                    {
                    }
    					
			    }
    			
			    /*
			     * If number was found
			     */
    			
			    if (numEnd >= 0)
				    if (pos > 0) {
					    c = newExpressionString[pos-1];
    				
					    if (
							    ( c != ',' ) &&
							    ( c != ';' ) &&
							    ( c != '|' ) &&
							    ( c != '&' ) &&
							    ( c != '+' ) &&
							    ( c != '-' ) &&
							    ( c != '*' ) &&
							    ( c != '\\' ) &&
							    ( c != '/' ) &&
							    ( c != '%' ) &&
							    ( c != '(' ) &&
							    ( c != ')' ) &&
							    ( c != '=' ) &&
							    ( c != '>' ) &&
							    ( c != '<' ) &&
							    ( c != '~' ) &&
							    ( c != '^' ) &&
							    ( c != '!' )	)
						    numEnd = -1;
    					
    						
				    }
    			
			    if (numEnd >= 0) {


				    /*
				     * If preceding word was unknown
				     * 
				     * For example:
				     *    'abc-2'
				     *   
				     *   number starts with '-', preceding word 'abc'
				     *   is not known by the parser 
				     */
				    if ( (matchStatusPrev == NOT_FOUND) && (pos > 0) ) {
    					
					    /*
					     * add preceding word to the list of tokens
					     * as unknown key word word
					     */
                        tokenStr = newExpressionString.Substring(lastPos, pos-lastPos);
					    addToken(tokenStr, new KeyWord());
    					
				    }				
    				
    				
				    /*
				     * Check leading operators ('-' or '+')
				     * 
				     * For example:
				     *    '2-1' :  1(num) -(op) 2(num) = 1(num)  
				     *    -1+2  : -1(num) +(op) 2(num) = 1(num) 
				     */
				    char fc = newExpressionString[pos];
    				
				    bool leadingOp = true;
				    if ( (fc == '-') || (fc == '+') ) {
					    if (initialTokens.Count > 0) {
						    Token lastToken = initialTokens[initialTokens.Count-1];
    						
						    if (	(lastToken.tokenTypeId == Operator.TYPE_ID) ||
								    ( (lastToken.tokenTypeId == ParserSymbol.TYPE_ID) && (lastToken.tokenId == ParserSymbol.LEFT_PARENTHESES_ID) )) 	
							    leadingOp = false;
						     else leadingOp = true;
    						
					    } else leadingOp = false;
     
				    } else leadingOp = false;
    				
    				
				    /*
				     * If leading operator was found
				     */
				    if (leadingOp == true) {
    					
					    /*
					     * Add leading operator to the tokens list
					     */
					    if ( fc == '-')
						    addToken("-", keyWordsList[minusKwId] );

					    if ( fc == '+')
						    addToken("+", keyWordsList[plusKwId] );
    				
					    pos++;
    					
    					
				    }
    				
				    /*
				     * Add found number to the tokens list
				     */
				    tokenStr = newExpressionString.Substring(pos, numEnd+1-pos);
				    addToken(tokenStr, keyWordsList[numberKwId] );
    				
				    /*
				     * change current position (just after the numeber ends)
				     */
				    pos = numEnd+1;
				    lastPos = pos;
    				
				    /*
				     * Mark match status indicators
				     */
				    matchStatus = FOUND;
				    matchStatusPrev = FOUND;
    				
			    } else {
			    /*
			     * If there is no number wich starts with current position
			     */
    				
    				
				    /*
				     * Check for known key words
				     */
				    int kwId = -1;
				    matchStatus = NOT_FOUND;
    				
				    KeyWord kw;
				    String sub = "";
				    String kwStr = "";
				    do {
    					
					    kwId++;
					    kw = keyWordsList[kwId];
					    kwStr = kw.wordString;
    					
					    if (pos + kwStr.Length <= newExpressionString.Length) {
    						
						    sub = newExpressionString.Substring(pos, kwStr.Length );					
    				
						    if (sub.Equals(kwStr))						
							    matchStatus = FOUND;
    						
					    }
    					
				    } while ( (kwId < keyWordsList.Count-1) && (matchStatus == NOT_FOUND) );
    				
    				
				    /*
				     * If key word is known by the parser
				     */
    				
				    if (matchStatus == FOUND)
    					
					    if (	
							    (kw.wordTypeId == Argument.TYPE_ID) ||	
							    (kw.wordTypeId == RecursiveArgument.TYPE_ID) ||	
							    (kw.wordTypeId == Function1Arg.TYPE_ID) ||	
							    (kw.wordTypeId == Function2Arg.TYPE_ID) ||	
							    (kw.wordTypeId == Function3Arg.TYPE_ID) ||	
							    (kw.wordTypeId == Const.TYPE_ID) ||	
							    (kw.wordTypeId == Constant.TYPE_ID) ||	
							    (kw.wordTypeId == Function.TYPE_ID) ||	
							    (kw.wordTypeId == Calculus.TYPE_ID)	)
    						
						    if ( pos + kwStr.Length < newExpressionString.Length ) {
    							
							    c = newExpressionString[pos + kwStr.Length];
    							
							    if (
									    ( c != ',' ) &&
									    ( c != ';' ) &&
									    ( c != '|' ) &&
									    ( c != '&' ) &&
									    ( c != '+' ) &&
									    ( c != '-' ) &&
									    ( c != '*' ) &&
									    ( c != '\\' ) &&
									    ( c != '/' ) &&
									    ( c != '%' ) &&
									    ( c != '(' ) &&
									    ( c != ')' ) &&
									    ( c != '=' ) &&
									    ( c != '>' ) &&
									    ( c != '<' ) &&
									    ( c != '~' ) &&
									    ( c != '^' ) &&
									    ( c != '!' )	)
    								
								    matchStatus = NOT_FOUND;
    							
						    }
    				
    				
				    if (matchStatus == FOUND) {
    					
					    /*
					     * if preceding word was not known by the parser
					     */
					    if ( (matchStatusPrev == NOT_FOUND) && (pos > 0) ) {
    						
						    /*
						     * Add preceding word to the tokens list
						     * as unknown key word
						     */
                            tokenStr = newExpressionString.Substring(lastPos, pos - lastPos);
						    addToken(tokenStr, new KeyWord());
    						
					    }
					    matchStatusPrev = FOUND;
    	
					    /*
					     * Add current (known by the parser)
					     * key word to the tokens list
					     */
					    tokenStr = newExpressionString.Substring(pos, kwStr.Length);
					    addToken(tokenStr, kw);
    					
					    /*
					     * Remember position where las adeed word ends + 1
					     */
					    lastPos = pos+kwStr.Length;
    					
					    /*
					     * Change current position;
					     */
					    pos = pos + kwStr.Length; 
				    } else {		
    					
					    /*
					     * Update preceding word indicator
					     */
					    matchStatusPrev = NOT_FOUND;
    					
					    /*
					     * Increment position if possible
					     */
					    if (pos < newExpressionString.Length)
						    pos++;
				    }
			    }
    			
		    /*
		     * while there is still something to analyse
		     */
		    } while (pos < newExpressionString.Length);
    		
    		
		    /*
		     * If key word was not known by the parser
		     * and end with the string end
		     * it needs to be added to the tokens list
		     * as unknown key word
		     */
		    if (matchStatus == NOT_FOUND) {
                tokenStr = newExpressionString.Substring(lastPos, pos - lastPos);
			    addToken(tokenStr, new KeyWord());
		    }
    		
    		
		    /*
		     * Evaluate tokens levels
		     * 
		     * token level identifies the sequance of parsing
		     */
		    evaluateTokensLevels();
    			
	    }
    	
    	
	    /**
	     * Evaluates tokens levels
	     */			
	    private void evaluateTokensLevels() {

		    int tokenLevel = 0;

		    Stack<TokenStackElement> tokenStack =  new Stack<TokenStackElement>();
		    bool precedingFunction = false;
    		
		    if (initialTokens.Count > 0)
			    for (int tokenIndex = 0; tokenIndex < initialTokens.Count; tokenIndex++) {
				    Token token = initialTokens[tokenIndex];		
    				
				    if (	( token.tokenTypeId == Function1Arg.TYPE_ID ) ||
						    ( token.tokenTypeId == Function2Arg.TYPE_ID ) ||
						    ( token.tokenTypeId == Function3Arg.TYPE_ID )	||
						    ( token.tokenTypeId == Function.TYPE_ID )	||
						    ( token.tokenTypeId == Calculus.TYPE_ID ) ||
						    ( token.tokenTypeId == RecursiveArgument.TYPE_ID ) ||
						    ( token.tokenTypeId == SpecialFunction.TYPE_ID )
						    ) {
    					
					    tokenLevel++;
					    precedingFunction = true;
    										
				    } else
				    if ((token.tokenTypeId == ParserSymbol.TYPE_ID) && (token.tokenId == ParserSymbol.LEFT_PARENTHESES_ID)) {
					    tokenLevel++;
    					
					    TokenStackElement stackEl = new TokenStackElement();
    					
					    stackEl.tokenId = token.tokenId;
					    stackEl.tokenIndex = tokenIndex;
					    stackEl.tokenLevel = tokenLevel;
					    stackEl.tokenTypeId = token.tokenTypeId;
					    stackEl.precedingFunction = precedingFunction;
					    tokenStack.Push(stackEl);
    					
					    precedingFunction = false;
    					
    					
				    } else
					    precedingFunction = false;
    				
				    token.tokenLevel = tokenLevel;
    				
				    if ((token.tokenTypeId == ParserSymbol.TYPE_ID) && (token.tokenId == ParserSymbol.RIGHT_PARENTHESES_ID)) {
					    tokenLevel--;
    					
					    TokenStackElement stackEl = tokenStack.Pop();
    					
					    if (stackEl.precedingFunction == true)
						    tokenLevel--;
    					
    					
				    }				
    				
			    }
    		
	    }
    	
	    /**
	     * copy initial tokens lito to tokens list
	     */
	    private void copyInitialTokens() {

		    tokensList = new List<Token>();
    		
		    foreach (Token token in initialTokens) {
			    tokensList.Add(token.clone());
		    }

	    }
    	
	    /*
	     * Text adjusting.
	     */
	    private String getLeftSpaces(String maxStr, String str) {

		    String spc = "";
		    for (int i=0; i<maxStr.Length - str.Length; i++)
			    spc = spc + " ";
		    return spc + str;

	    }
    	
	    /*
	     * Text adjusting.
	     */
	    private String getRightSpaces(String maxStr, String str) {

		    String spc = "";
		    for (int i=0; i<maxStr.Length - str.Length; i++)
			    spc = " " + spc;
		    return str + spc;

	    }
    	
	    /**
	     * Shows parsing (verbose mode purposes).
	     * 
	     */
	    private void showParsing(int lPos, int rPos) {

		    mXparser.consolePrint(" ---> ");
		    for (int i=lPos; i<=rPos; i++) {
			    Token token = tokensList[i];
    			
			    if (token.tokenTypeId == ParserSymbol.NUMBER_TYPE_ID)
				    mXparser.consolePrint(token.tokenValue + " ");
			    else
				    mXparser.consolePrint(token.tokenStr + " ");
		    }
    		
		    mXparser.consolePrint(" ... ");
    		
	    }
    	
    	
	    /**
	     * shows known keywords 
	     */
	    void showKeyWords() {

		    int keyWordsNumber = keyWordsList.Count;
		    String maxStr = "KEY_WORD";
		    mXparser.consolePrintln("KEY WORDS:");
		    mXparser.consolePrintln(" -------------------------------------------");	
		    mXparser.consolePrintln("|      IDX | KEY_WORD |       ID |  TYPE_ID |");
		    mXparser.consolePrintln(" -------------------------------------------");	
		    for (int keyWordIndex=0; keyWordIndex<keyWordsNumber; keyWordIndex++){
			    KeyWord keyWord = keyWordsList[keyWordIndex];
    			
			    String idxStr = getLeftSpaces(maxStr, keyWordIndex.ToString());
			    String wordStr = getLeftSpaces(maxStr, keyWord.wordString);
			    String idStr = getLeftSpaces(maxStr, keyWord.wordId.ToString());
			    String typeIdStr = getLeftSpaces(maxStr, keyWord.wordTypeId.ToString());
    			
			    mXparser.consolePrintln("| " + idxStr+ " | " + wordStr + " | " + idStr + " | " + typeIdStr + " |");			
		    }
		    mXparser.consolePrintln(" -------------------------------------------");	

	    }

	    /**
	     * Gets help content.
	     * 
	     * @return     The help content.
	     */
	    public String getHelp() {

		    return getHelp("");
	    }	
    	
	    /**
	     * Searching help content.
	     * 
	     * @param      word                searching key word
	     * 
	     * @return     The help content.
	     */
	    public String getHelp(String word) {

		    keyWordsList = new List<KeyWord>();
		    String helpStr = "Help content: \n\n";
    		
    		
		    addParserKeyWords();
		    addArgumentsKeyWords();
		    addFunctionsKeyWords();
		    addConstantsKeyWords();		
		    keyWordsList.Sort( new KwTypeComparator() );
    		
		    int keyWordsNumber = keyWordsList.Count;
		    String type, kw;
		    String line;
		    for (int keyWordIndex=0; keyWordIndex<keyWordsNumber; keyWordIndex++){
			    KeyWord keyWord = keyWordsList[keyWordIndex];
    			
			    type = "";
			    kw = keyWord.wordString;
    			
    			
			    if ( (keyWord.wordTypeId == ParserSymbol.NUMBER_TYPE_ID) )
				    kw = "_number_";
    			
    			
			    if (keyWord.wordTypeId == Operator.TYPE_ID)
				    type = "OPERATOR";
			    if (keyWord.wordTypeId == BinaryRelation.TYPE_ID)
				    type = "BINARY_RELATION";
			    if (keyWord.wordTypeId == Argument.TYPE_ID)
				    type = "ARGUMENT";
			    if (keyWord.wordTypeId == RecursiveArgument.TYPE_ID)
				    type = "RECURSIVE_ARGUMENT";
			    if (keyWord.wordTypeId == Const.TYPE_ID)
				    type = "CONSTANT";
			    if (keyWord.wordTypeId == Constant.TYPE_ID)
				    type = "USER_CONSTANT";
			    if (keyWord.wordTypeId == Function1Arg.TYPE_ID)
				    type = "FUNCTION_1_ARG";
			    if (keyWord.wordTypeId == Function2Arg.TYPE_ID)
				    type = "FUNCTION_2_ARG";
			    if (keyWord.wordTypeId == Function3Arg.TYPE_ID)
				    type = "FUNCTION_3_ARG";
			    if (keyWord.wordTypeId == Calculus.TYPE_ID)
				    type = "CALCULUS / OPERATOR";
			    if (keyWord.wordTypeId == ParserSymbol.TYPE_ID)
				    type = "PARSER";
			    if (keyWord.wordTypeId == ParserSymbol.NUMBER_TYPE_ID)
				    type = "NUMBER";
			    if (keyWord.wordTypeId == Function.TYPE_ID)
				    type = "USER_FUNCTION";
			    if (keyWord.wordTypeId == SpecialFunction.TYPE_ID)
				    type = "SPECIAL_FUNCTION";
			    if (keyWord.wordTypeId == BooleanOperator.TYPE_ID)
				    type = "BOOLEAN_OPERATOR";
    						
			    line = getLeftSpaces("12345",(keyWordIndex+1).ToString()) + ". " + 
			    getRightSpaces("0123456789", kw) + getRightSpaces("                        ","<" + type + ">") + keyWord.description + "\n";
    			
			    if ( (line.ToLower().IndexOf(word.ToLower()) >= 0) ){
				    helpStr = helpStr + line;
			    }
    			
		    }

		    return helpStr;
	    }		
    	
	    /*
	     * shows tokens
	     */
	    void showTokens() {
		    showTokens(tokensList);
	    }

	    /*
	     * show tokens
	     */
	    void showTokens(List<Token> tokensList) {

		    String maxStr = "PartTypeId";
		    int tokensNumber = tokensList.Count;
		    mXparser.consolePrintln(" -----------------------------------------");
		    mXparser.consolePrintln("| Expression Partitions: " + expressionString );
		    mXparser.consolePrintln(" -----------------------------------------------------------------------------------------");
		    mXparser.consolePrintln("|    PartIdx |       Part |       KeyW |     PartId | PartTypeId |  PartLevel |  PartValue |");
		    mXparser.consolePrintln(" -----------------------------------------------------------------------------------------");
    		
		    for (int tokenIndex=0; tokenIndex < tokensNumber; tokenIndex++){
    			
			    String tokenIndexStr = getLeftSpaces(maxStr, tokenIndex.ToString() );
			    String tokenStr = getLeftSpaces(maxStr, tokensList[tokenIndex].tokenStr );
			    String keyWordStr = getLeftSpaces(maxStr, tokensList[tokenIndex].keyWord );
			    String tokenIdStr = getLeftSpaces(maxStr, tokensList[tokenIndex].tokenId.ToString() );
			    String tokenTypeIdStr = getLeftSpaces(maxStr, tokensList[tokenIndex].tokenTypeId.ToString() );
			    String tokenLevelStr = getLeftSpaces(maxStr, tokensList[tokenIndex].tokenLevel.ToString() );
			    String tokenValueStr = getLeftSpaces(maxStr, tokensList[tokenIndex].tokenValue.ToString() );
    			
			    mXparser.consolePrintln(	"| " + tokenIndexStr +
								    " | " + tokenStr +
								    " | " + keyWordStr +
								    " | " + tokenIdStr +
								    " | " + tokenTypeIdStr +
								    " | " + tokenLevelStr +
								    " | " + tokenValueStr + " |");		
		    }
		    mXparser.consolePrintln(	" -----------------------------------------------------------------------------------------");

	    }

	    /**
	     * shows initial tokens
	     */
	    void showInitialTokens() {

		    String maxStr = "PartTypeId";
		    int tokensNumber = initialTokens.Count;
		    mXparser.consolePrintln(" -----------------------------------------");
		    mXparser.consolePrintln("| Expression Partitions: " + expressionString );
		    mXparser.consolePrintln(" -----------------------------------------------------------------------------------------");
		    mXparser.consolePrintln("|    PartIdx |       Part |       KeyW |     PartId | PartTypeId |  PartLevel |  PartValue |");
		    mXparser.consolePrintln(" -----------------------------------------------------------------------------------------");
    		
		    for (int tokenIndex=0; tokenIndex < tokensNumber; tokenIndex++){
    			
			    String tokenIndexStr = getLeftSpaces(maxStr, tokenIndex.ToString() );
			    String tokenStr = getLeftSpaces(maxStr, initialTokens[tokenIndex].tokenStr );
			    String keyWordStr = getLeftSpaces(maxStr, initialTokens[tokenIndex].keyWord );
			    String tokenIdStr = getLeftSpaces(maxStr, initialTokens[tokenIndex].tokenId.ToString() );
			    String tokenTypeIdStr = getLeftSpaces(maxStr, initialTokens[tokenIndex].tokenTypeId.ToString() );
			    String tokenLevelStr = getLeftSpaces(maxStr, initialTokens[tokenIndex].tokenLevel.ToString() );
			    String tokenValueStr = getLeftSpaces(maxStr, initialTokens[tokenIndex].tokenValue.ToString() );
    			
			    mXparser.consolePrintln(	"| " + tokenIndexStr +
								    " | " + tokenStr +
								    " | " + keyWordStr +
								    " | " + tokenIdStr +
								    " | " + tokenTypeIdStr +
								    " | " + tokenLevelStr +
								    " | " + tokenValueStr + " |");		
		    }
		    mXparser.consolePrintln(	" -----------------------------------------------------------------------------------------");

	    }	
    	
	    /*
	     * show arguments
	     */
	    private void showArguments() {
    		
		    foreach (Argument a in argumentsList) {
    			
			    bool vMode = a.getVerboseMode();
			    a.setSilentMode();
			    printSystemInfo(a.getArgumentName() + " = " + a.getArgumentValue() + "\n", WITH_EXP_STR);
    			
			    if (vMode == true)
				    a.setVerboseMode();
    			
		    }
    		
	    }
    	
	    /**
	     * 
	     * @param info
	     * @param withExpressionString
	     */
	    private void printSystemInfo(String info, bool withExpressionString) {

		    if (withExpressionString)
			    mXparser.consolePrint( /*"[" + this +  "]" +  */ "[" + description + "]" + "[" + expressionString + "] " + info);
		    else
			    mXparser.consolePrint(/*"[" + this +  "]" + */ info);

	    }
    	

	    /**
	     * License info.
	     */
	    public const String LICENSE =
		    "                      mXparser - version " + VERSION + "\n" +
		    "         A flexible mathematics eXpressions parser for C#.\n" +
            "                   (port from mXparser for JAVA)\n" +
            "\n" +
            "You may use this software under the condition of Simplified BSD License:\n" +
		    "\n" +
		    "Copyright 2010 MARIUSZ GROMADA. All rights reserved.\n" +
		    "\n" +
		    "Redistribution and use in source and binary forms, with or without modification, are\n" +
		    "permitted provided that the following conditions are met:\n" +
		    "\n" +
		    "   1. Redistributions of source code must retain the above copyright notice, this list of\n" +
		    "      conditions and the following disclaimer.\n" +
		    "\n" +
		    "   2. Redistributions in binary form must reproduce the above copyright notice, this list\n" +
		    "      of conditions and the following disclaimer in the documentation and/or other materials\n" +
		    "      provided with the distribution.\n" +
		    "\n" +
		    "THIS SOFTWARE IS PROVIDED BY MARIUSZ GROMADA ``AS IS'' AND ANY EXPRESS OR IMPLIED\n" +
		    "WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND\n" +
		    "FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL MARIUSZ GROMADA OR\n" +
		    "CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR\n" +
		    "CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR\n" +
		    "SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON\n" +
		    "ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING\n" +
		    "NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF\n" +
		    "ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.\n" +
		    "\n" +
		    "The views and conclusions contained in the software and documentation are those of the\n" +
		    "authors and should not be interpreted as representing official policies, either expressed\n" +
		    "or implied, of MARIUSZ GROMADA.\n" +
		    "\n" +
		    "If you have any questions/bugs feel free to contact:\n" +
		    "\n" +
		    "    Mariusz Gromada\n" +
		    "    mariusz.gromada@mathspace.pl\n" +
		    "    http://mathspace.pl/\n" +
		    "    http://mxparser.sourceforge.net/\n"
		    ;		
    	
	    /**
	     * Gets license info
	     * 
	     * @return     License info as string.
	     */
	    public String getLicense() {
		    return Expression.LICENSE;
	    }		


	    /**
	     * Expression cloning.
	     */
	    internal Expression clone() {
		    Expression newExp = new Expression(this);
		    if ( (initialTokens != null) && (initialTokens.Count > 0) )
			    newExp.initialTokens = createInitialTokens(0, initialTokens.Count-1, initialTokens);
		    return newExp;
	    }
    	
    }



    /**
     * Token - base class for tokens definition 
     */
    internal class Token {

        /**
         * String token
         */
        internal String tokenStr;

        /**
         * Key word string (if matched)
         * 
         * Please refer to below interfaces
         *    Operator
         *    Function1Arg
         *    Function2Arg
         *    Function3Arg
         *    BinaryRelation
         *    Const
         *    ParserSymbol
         */
        internal String keyWord;

        /**
         * Partition identifier
         * 
         * Please refer to below interfaces
         *    Operator
         *    Function1Arg
         *    Function2Arg
         *    Function3Arg
         *    BinaryRelation
         *    Const
         *    ParserSymbol
         */
        internal int tokenId;

        /**
         * Partition type
         * 
         * Please refer to below interfaces
         *    Operator
         *    Function1Arg
         *    Function2Arg
         *    Function3Arg
         *    BinaryRelation
         *    Const
         *    ParserSymbol
         */
        internal int tokenTypeId;

        /**
         * Partition level
         */
        internal int tokenLevel;

        /**
         * Partition value if number
         */
        internal double tokenValue;

        /**
         * Default constructor
         */
        internal Token() {
            tokenStr = "";
            keyWord = "";
            tokenId = Const.NaN;
            tokenTypeId = Const.NaN;
            tokenLevel = Const.NaN;
            tokenValue = Double.NaN;
        }

        public Token clone() {
            Token token = new Token();

            token.keyWord = keyWord;
            token.tokenStr = tokenStr;
            token.tokenId = tokenId;
            token.tokenLevel = tokenLevel;
            token.tokenTypeId = tokenTypeId;
            token.tokenValue = tokenValue;

            return token;
        }


    }



    /*=================================================
     * 
     * Package level classes and interfaces 
     * 
     *================================================= 
     */



    /**
     * Package level class for retriving calculus parameters
     * Holds params number and partameter string
     */
    internal class FunctionParameter {

        /**
         * 
         */
        internal List<Token> tokens;

        /**
         * 
         */
        internal String paramStr;
        internal int fromIndex;
        internal int toIndex;



        /**
         * 
         * @param paramStr
         * @param parametersNumber
         */
        internal FunctionParameter(List<Token> tokens,
                String paramStr,
                int fromIndex,
                int toIndex) {
            this.tokens = tokens;
            this.paramStr = paramStr;
            this.fromIndex = fromIndex;
            this.toIndex = toIndex;
        }

    }


    /**
     * Package level class
     * 
     */


    internal class ArgumentParameter {

        internal Argument argument;
        internal double initialValue;
        internal int initialType;
        internal int presence;
        internal int index;

        internal ArgumentParameter() {
            argument = null;
            initialValue = Double.NaN;
            initialType = Const.NaN;
            presence = Expression.NOT_FOUND;
        }

    }

    /**
     * Base class prepresenting key words knwon by the parsere
     */
    internal class KeyWord {

        internal String wordString;
        internal int wordId;
        internal int wordTypeId;
        internal String description;

        internal KeyWord() {
            wordString = "";
            wordId = Const.NaN;
            wordTypeId = Const.NaN;
            description = "";
        }

        /**
         * Constructor - creates key words form wordStrin wordId
         * and wordTypId
         * 
         * @param wordString   the word string (refere to below interfaces)
         * @param wordId       the word identifier (refere to below interfaces)
         * @param wordTypeId   the word type (refere to below interfaces)
         */
        internal KeyWord(String wordString, int wordId, int wordTypeId) {
            this.wordString = wordString;
            this.wordId = wordId;
            this.wordTypeId = wordTypeId;
        }

        internal KeyWord(String wordString, String description, int wordId, int wordTypeId) {
            this.wordString = wordString;
            this.wordId = wordId;
            this.wordTypeId = wordTypeId;
            this.description = description;
        }

    }

    /**
     * Internal token class
     * which is used with stack while
     * evaluation of tokens levels
     */
    internal class TokenStackElement {

        internal int tokenIndex;
        internal int tokenId;
        internal int tokenTypeId;
        internal int tokenLevel;
        internal bool precedingFunction;

    }


    internal class SyntaxStackElement {

        internal String tokenStr;
        internal int tokenLevel;

        internal SyntaxStackElement(String tokenStr, int tokenLevel) {

            this.tokenStr = tokenStr;
            this.tokenLevel = tokenLevel;

        }

    }


    /*
     * ---------------------------------------------------------
     * Comparators for sorting 
     * ---------------------------------------------------------
     */

    /**
     * Comparator for key word list sorting by key word string.
     * This king of sorting is used while checking the syntax
     * (duplicated key word error)
     */
    class KwStrComparator : IComparer<KeyWord> {

        /**
         * 
         */
        public int Compare(KeyWord kw1, KeyWord kw2) {
            String s1 = kw1.wordString;
            String s2 = kw2.wordString;
            return s1.CompareTo(s2);
        }

    }


    /**
     * Comparator for key word list sorting by
     * descending key word length
     * .
     * This king of sorting is used while tokenizing
     * (best match)
     */
    class DescKwLenComparator : IComparer<KeyWord> {

        /**
         * 
         */
        public int Compare(KeyWord kw1, KeyWord kw2) {

            int l1 = kw1.wordString.Length;
            int l2 = kw2.wordString.Length;

            return l2 - l1;

        }

    }


    /**
     * Comparator for key word list sorting by
     * type of the key word
     * 
     */
    class KwTypeComparator : IComparer<KeyWord> {

        /**
         * 
         */
        public int Compare(KeyWord kw1, KeyWord kw2) {

            int t1 = kw1.wordTypeId * 1000 + kw1.wordId;
            int t2 = kw2.wordTypeId * 1000 + kw2.wordId;

            return t1 - t2;

        }

    }


    /*
     * ---------------------------------------------------------
     * Grouping constants by interfaces 
     * ---------------------------------------------------------
     */


    /**
     * ParserSymbol
     * 
     * Identifiers and strings (words) definition.
     * Used mailny by the addParserKeywords() 
     */
    internal sealed class ParserSymbol {


        internal const String Digits = "([0-9]+)";
        internal const String HexDigits = "(([0-9]|[A-F])+)";
        internal const String Exp = "[eE][+-]?" + Digits;
        internal const String fpRegex =
                ("[\\x00-\\x20]*" +  // Optional leading "whitespace"
                "[+-]?(" + // Optional sign character
                "NaN|" +           // "NaN" string
                "Infinity|" +      // "Infinity" string
                "(((" + Digits + "(\\.)?(" + Digits + "?)(" + Exp + ")?)|" +
                "(\\.(" + Digits + ")(" + Exp + ")?)|" +
                "((" +
            // 0[xX] HexDigits ._opt BinaryExponent FloatTypeSuffix_opt
                "(0[xX]" + HexDigits + "(\\.)?)|" +
            // 0[xX] HexDigits_opt . HexDigits BinaryExponent FloatTypeSuffix_opt
                "(0[xX]" + HexDigits + "?(\\.)" + HexDigits + ")" +
                ")[pP][+-]?" + Digits + "))" +
                "[fFdD]?))" +
                "[\\x00-\\x20]*");

        internal const String NOT_NUM = "[^0-9.-]";
        internal const String TWO_DOT = "[0-9]*[.][0-9]*[.][0-9]*";
        internal const String TWO_MIN = "[0-9]*[-][0-9]*[-][0-9]*";
        internal const String REAL = "^([-]|[.]|[-.]|[0-9])[0-9]*[.]*[0-9]+$";
        internal const String INTEGER = "^([-])[0-9]+$";
        internal const String NUMBER = fpRegex;


        internal const int TYPE_ID = 11;
        internal const int LEFT_PARENTHESES_ID = 1;
        internal const int RIGHT_PARENTHESES_ID = 2;
        internal const int COMMA_ID = 3;
        internal const int NUMBER_ID = 1;
        internal const int NUMBER_TYPE_ID = 0;



        internal const String LEFT_PARENTHESES_STR = "(";
        internal const String RIGHT_PARENTHESES_STR = ")";
        internal const String COMMA_STR = ",";
        internal const String SEMI_STR = ";";
        internal const String NUMBER_STR = "_num_";
        internal const String NUMBER_REG_EXP = NUMBER;

        internal const String LEFT_PARENTHESES_DESC = "left parentheses";
        internal const String RIGHT_PARENTHESES_DESC = "right parentheses";
        internal const String COMMA_DESC = "comma (function parameters)";
        internal const String SEMI_DESC = "semicolon (function parameters)";
        internal const String NUMBER_DESC = "decimal number";
        internal const String NUMBER_REG_DESC = "regullar expression for decimal numbers";


    }

    /**
     * Operator
     * 
     * Identifiers and strings (words) definition.
     * Used mailny by the addParserKeywords() 
     */
    internal sealed class Operator {


        internal const int TYPE_ID = 1;

        internal const int PLUS_ID = 1;
        internal const int MINUS_ID = 2;
        internal const int MULTIPLY_ID = 3;
        internal const int DIVIDE_ID = 4;
        internal const int POWER_ID = 5;

        internal const int FACT_ID = 6;
        internal const int MOD_ID = 7;



        internal const String PLUS_STR = "+";
        internal const String MINUS_STR = "-";
        internal const String MULTIPLY_STR = "*";
        internal const String DIVIDE_STR = "/";
        internal const String POWER_STR = "^";


        internal const String FACT_STR = "!";
        internal const String MOD_STR = "%";


        internal const String PLUS_DESC = "addition";
        internal const String MINUS_DESC = "subtraction";
        internal const String MULTIPLY_DESC = "multiplication";
        internal const String DIVIDE_DESC = "division";
        internal const String POWER_DESC = "exponentiation";
        internal const String FACT_DESC = "factorial";
        internal const String MOD_DESC = "modulo function";

    }


    internal sealed class BooleanOperator {


        internal const int TYPE_ID = 2;

        internal const int AND_ID = 1;
        internal const int NAND_ID = 2;
        internal const int OR_ID = 3;
        internal const int NOR_ID = 4;
        internal const int XOR_ID = 5;
        internal const int IMP_ID = 6;
        internal const int CIMP_ID = 7;
        internal const int NIMP_ID = 8;
        internal const int CNIMP_ID = 9;
        internal const int EQV_ID = 10;
        internal const int NEG_ID = 11;


        internal const String NEG_STR = "~";

        internal const String AND_STR = "&";
        internal const String AND1_STR = "&&";
        internal const String AND2_STR = "/\\";
        internal const String NAND_STR = NEG_STR + AND_STR;
        internal const String NAND1_STR = NEG_STR + AND1_STR;
        internal const String NAND2_STR = NEG_STR + AND2_STR;

        internal const String OR_STR = "|";
        internal const String OR1_STR = "||";
        internal const String OR2_STR = "\\/";
        internal const String NOR_STR = NEG_STR + OR_STR;
        internal const String NOR1_STR = NEG_STR + OR1_STR;
        internal const String NOR2_STR = NEG_STR + OR2_STR;
        internal const String XOR_STR = "(+)";
        internal const String IMP_STR = "->";
        internal const String CIMP_STR = "<-";
        internal const String NIMP_STR = "-/>";
        internal const String CNIMP_STR = "</-";
        internal const String EQV_STR = "<->";

        internal const String AND_DESC = "logical conjunction (AND)";
        internal const String OR_DESC = "logical disjunction (OR)";
        internal const String NEG_DESC = "negation";

        internal const String NAND_DESC = "NAND - Sheffer stroke";
        internal const String NOR_DESC = "logical NOR";
        internal const String XOR_DESC = "exclusive or (XOR)";
        internal const String IMP_DESC = "implication (IMP)";
        internal const String CIMP_DESC = "converse implication (CIMP)";
        internal const String NIMP_DESC = "material nonimplication (NIMP)";
        internal const String CNIMP_DESC = "converse nonimplication (CNIMP)";
        internal const String EQV_DESC = "logical biconditional (EQV)";


    }



    /**
     * BinaryRelation
     * 
     * Identifiers and strings (words) definition.
     * Used mailny by the addParserKeywords() 
     */

    internal sealed class BinaryRelation {


        internal const int TYPE_ID = 3;
        internal const int EQ_ID = 1;
        internal const int NEQ_ID = 2;
        internal const int LT_ID = 3;
        internal const int GT_ID = 4;
        internal const int LEQ_ID = 5;
        internal const int GEQ_ID = 6;


        internal const String EQ_STR = "=";
        internal const String EQ1_STR = "==";

        internal const String NEQ_STR = "<>";
        internal const String NEQ1_STR = "~=";
        internal const String NEQ2_STR = "!=";

        internal const String LT_STR = "<";

        internal const String GT_STR = ">";

        internal const String LEQ_STR = "<=";

        internal const String GEQ_STR = ">=";

        internal const String EQ_DESC = "equality";
        internal const String NEQ_DESC = "inequation";
        internal const String LT_DESC = "lower than";
        internal const String GT_DESC = "greater than";
        internal const String LEQ_DESC = "lower or equal";
        internal const String GEQ_DESC = "greater or equal";

    }




    /**
     * Function1Arg
     * 
     * Identifiers and strings (words) definition.
     * Used mailny by the addParserKeywords() 
     */
    internal sealed class Function1Arg {

        internal const int TYPE_ID = 4;
        internal const int SIN_ID = 1;
        internal const int COS_ID = 2;
        internal const int TAN_ID = 3;
        internal const int CTAN_ID = 4;
        internal const int SEC_ID = 5;
        internal const int COSEC_ID = 6;
        internal const int ASIN_ID = 7;
        internal const int ACOS_ID = 8;
        internal const int ATAN_ID = 9;
        internal const int ACTAN_ID = 10;
        internal const int LN_ID = 11;
        internal const int LOG2_ID = 12;
        internal const int LOG10_ID = 13;
        internal const int RAD_ID = 14;
        internal const int EXP_ID = 15;
        internal const int SQRT_ID = 16;
        internal const int SINH_ID = 17;
        internal const int COSH_ID = 18;
        internal const int TANH_ID = 19;
        internal const int COTH_ID = 20;
        internal const int SECH_ID = 21;
        internal const int CSCH_ID = 22;
        internal const int DEG_ID = 23;
        internal const int ABS_ID = 24;
        internal const int SGN_ID = 25;
        internal const int FLOOR_ID = 26;
        internal const int CEIL_ID = 27;
        internal const int NOT_ID = 29;
        internal const int ARSINH_ID = 30;
        internal const int ARCOSH_ID = 31;
        internal const int ARTANH_ID = 32;
        internal const int ARCOTH_ID = 33;
        internal const int ARSECH_ID = 34;
        internal const int ARCSCH_ID = 35;

        internal const int SA_ID = 36;
        internal const int SINC_ID = 37;

        internal const int BELL_NUMBER_ID = 38;
        internal const int LUCAS_NUMBER_ID = 39;
        internal const int FIBONACCI_NUMBER_ID = 40;
        internal const int HARMONIC_NUMBER_ID = 41;


        internal const String SIN_STR = "sin";
        internal const String COS_STR = "cos";
        internal const String TAN_STR = "tan";
        internal const String TG_STR = "tg";
        internal const String CTAN_STR = "ctan";
        internal const String CTG_STR = "ctg";
        internal const String COT_STR = "cot";

        internal const String SEC_STR = "sec";
        internal const String COSEC_STR = "cosec";
        internal const String CSC_STR = "csc";

        internal const String ASIN_STR = "asin";
        internal const String ARSIN_STR = "arsin";
        internal const String ARCSIN_STR = "arcsin";

        internal const String ACOS_STR = "acos";
        internal const String ARCOS_STR = "arcos";
        internal const String ARCCOS_STR = "arccos";

        internal const String ATAN_STR = "atan";
        internal const String ARCTAN_STR = "arctan";
        internal const String ATG_STR = "atg";
        internal const String ARCTG_STR = "arctg";
        internal const String ACTAN_STR = "actan";
        internal const String ARCCTAN_STR = "arcctan";
        internal const String ACTG_STR = "actg";
        internal const String ARCCTG_STR = "arcctg";
        internal const String ACOT_STR = "acot";
        internal const String ARCCOT_STR = "arccot";

        internal const String LN_STR = "ln";
        internal const String LOG2_STR = "log2";
        internal const String LOG10_STR = "log10";

        internal const String RAD_STR = "rad";
        internal const String EXP_STR = "exp";
        internal const String SQRT_STR = "sqrt";

        internal const String SINH_STR = "sinh";
        internal const String COSH_STR = "cosh";
        internal const String TANH_STR = "tanh";
        internal const String TGH_STR = "tgh";
        internal const String CTANH_STR = "ctanh";
        internal const String COTH_STR = "coth";
        internal const String CTGH_STR = "ctgh";
        internal const String SECH_STR = "sech";
        internal const String CSCH_STR = "csch";
        internal const String COSECH_STR = "cosech";

        internal const String DEG_STR = "deg";
        internal const String ABS_STR = "abs";
        internal const String SGN_STR = "sgn";
        internal const String FLOOR_STR = "floor";
        internal const String CEIL_STR = "ceil";

        internal const String NOT_STR = "not";

        internal const String ASINH_STR = "asinh";
        internal const String ARSINH_STR = "arsinh";
        internal const String ARCSINH_STR = "arcsinh";

        internal const String ACOSH_STR = "acosh";
        internal const String ARCOSH_STR = "arcosh";
        internal const String ARCCOSH_STR = "arccosh";

        internal const String ATANH_STR = "atanh";
        internal const String ARCTANH_STR = "arctanh";

        internal const String ATGH_STR = "atgh";
        internal const String ARCTGH_STR = "arctgh";

        internal const String ACTANH_STR = "actanh";
        internal const String ARCCTANH_STR = "arcctanh";

        internal const String ACOTH_STR = "acoth";
        internal const String ARCOTH_STR = "arcoth";
        internal const String ARCCOTH_STR = "arccoth";

        internal const String ACTGH_STR = "actgh";
        internal const String ARCCTGH_STR = "arcctgh";

        internal const String ASECH_STR = "asech";
        internal const String ARSECH_STR = "arsech";
        internal const String ARCSECH_STR = "arcsech";

        internal const String ACSCH_STR = "acsch";
        internal const String ARCSCH_STR = "arcsch";
        internal const String ARCCSCH_STR = "arccsch";

        internal const String ACOSECH_STR = "acosech";
        internal const String ARCOSECH_STR = "arcosech";
        internal const String ARCCOSECH_STR = "arccosech";

        internal const String SA_STR = "sinc";
        internal const String SA1_STR = "Sa";
        internal const String SINC_STR = "Sinc";
        internal const String BELL_NUMBER_STR = "Bell";
        internal const String LUCAS_NUMBER_STR = "Luc";
        internal const String FIBONACCI_NUMBER_STR = "Fib";
        internal const String HARMONIC_NUMBER_STR = "harm";


        internal const String SIN_DESC = "trigonometric sine function";
        internal const String COS_DESC = "trigonometric cosine function";
        internal const String TAN_DESC = "trigonometric tangent function";
        internal const String CTAN_DESC = "trigonometric cotangent function";
        internal const String SEC_DESC = "trigonometric secant function";
        internal const String COSEC_DESC = "trigonometric cosecant function";
        internal const String ASIN_DESC = "inverse trigonometric sine function";
        internal const String ACOS_DESC = "inverse trigonometric cosine function";
        internal const String ATAN_DESC = "inverse trigonometric tangent function";
        internal const String ACTAN_DESC = "inverse trigonometric cotangent function";
        internal const String LN_DESC = "natural logarithm function (base e)";
        internal const String LOG2_DESC = "binary logarithm function (base 2)";
        internal const String LOG10_DESC = "common logarithm function (base 10)";
        internal const String RAD_DESC = "degrees to radians function";
        internal const String EXP_DESC = "exponential function";
        internal const String SQRT_DESC = "squre root function";
        internal const String SINH_DESC = "hyperbolic sine function";
        internal const String COSH_DESC = "hyperbolic cosine function";
        internal const String TANH_DESC = "hyperbolic tangent function";
        internal const String COTH_DESC = "hyperbolic cotangent function";
        internal const String SECH_DESC = "hyperbolic secant function";
        internal const String CSCH_DESC = "hyperbolic cosecant function";
        internal const String DEG_DESC = "radians to degrees function";
        internal const String ABS_DESC = "absolut value function";
        internal const String SGN_DESC = "signum function";
        internal const String FLOOR_DESC = "floor function";
        internal const String CEIL_DESC = "ceiling function";
        internal const String NOT_DESC = "negation function";
        internal const String ARSINH_DESC = "inverse hyperbolic sine function";
        internal const String ARCOSH_DESC = "inverse hyperbolic cosine function";
        internal const String ARTANH_DESC = "inverse hyperbolic tangent function";
        internal const String ARCOTH_DESC = "inverse hyperbolic cotangent function";
        internal const String ARSECH_DESC = "inverse hyperbolic secant function";
        internal const String ARCSCH_DESC = "inverse hyperbolic cosecant function";

        internal const String SA_DESC = "sinc function (normalized)";
        internal const String SINC_DESC = "sinc function (unnormalized)";
        internal const String BELL_NUMBER_DESC = "Bell number";
        internal const String LUCAS_NUMBER_DESC = "Lucas number";
        internal const String FIBONACCI_NUMBER_DESC = "Fionacci number";
        internal const String HARMONIC_NUMBER_DESC = "Harmonic number";


    }

    /**
     * Function2Arg
     * 
     * Identifiers and strings (words) definition.
     * Used mailny by the addParserKeywords() 
     */
    internal sealed class Function2Arg {


        internal const int TYPE_ID = 5;

        internal const int LOG_ID = 1;
        internal const int MOD_ID = 2;
        internal const int BINOM_COEFF_ID = 3;
        internal const int BERNOULLI_NUMBER_ID = 4;
        internal const int STIRLING1_NUMBER_ID = 5;
        internal const int STIRLING2_NUMBER_ID = 6;
        internal const int WORPITZKY_NUMBER_ID = 7;
        internal const int EULER_NUMBER_ID = 8;
        internal const int KRONECKER_DELTA_ID = 9;
        internal const int EULER_POLYNOMIAL_ID = 10;
        internal const int HARMONIC_NUMBER_ID = 11;


        internal const String LOG_STR = "log";
        internal const String MOD_STR = "mod";
        internal const String BINOM_COEFF_STR = "C";
        internal const String BERNOULLI_NUMBER_STR = "Bern";
        internal const String STIRLING1_NUMBER_STR = "Stirl1";
        internal const String STIRLING2_NUMBER_STR = "Stirl2";
        internal const String WORPITZKY_NUMBER_STR = "Worp";
        internal const String EULER_NUMBER_STR = "Euler";
        internal const String KRONECKER_DELTA_STR = "KDelta";
        internal const String EULER_POLYNOMIAL_STR = "EulerPol";
        internal const String HARMONIC_NUMBER_STR = "Harm";

        internal const String LOG_DESC = "logarithm function";
        internal const String MOD_DESC = "modulo function";
        internal const String BINOM_COEFF_DESC = "binomial coefficient function";
        internal const String BERNOULLI_NUMBER_DESC = "Bernoulli numbers";
        internal const String STIRLING1_NUMBER_DESC = "Stirling numbers of the first kind";
        internal const String STIRLING2_NUMBER_DESC = "Stirling numbers of the second kind";
        internal const String WORPITZKY_NUMBER_DESC = "Worpitzky number";
        internal const String EULER_NUMBER_DESC = "Euler number";
        internal const String KRONECKER_DELTA_DESC = "Kronecker delta";
        internal const String EULER_POLYNOMIAL_DESC = "EulerPol";
        internal const String HARMONIC_NUMBER_DESC = "Harmonic number";


    }

    /**
     * Function3Arg
     * 
     * Identifiers and strings (words) definition.
     * Used mailny by the addParserKeywords() 
     */
    internal sealed class Function3Arg {


        internal const int TYPE_ID = 6;
        internal const int IF_CONDITION_ID = 1;
        internal const int IF_ID = 2;

        internal const int CHI_ab_ID = 3;
        internal const int CHI_AB_ID = 4;
        internal const int CHI_Ab_ID = 5;
        internal const int CHI_aB_ID = 6;


        internal const String IF_STR = "if";

        internal const String CHI_ab_STR = "chi";
        internal const String CHI_AB_STR = "CHi";
        internal const String CHI_Ab_STR = "Chi";
        internal const String CHI_aB_STR = "cHi";

        internal const String IF_DESC = "if function ( if(con; if_true; if_false) )";
        internal const String CHI_ab_DESC = "Characteristic function for x in (a;b) - chi(x; a; b)";
        internal const String CHI_AB_DESC = "Characteristic function for x in [a;b] - CHi(x; a; b)";
        internal const String CHI_Ab_DESC = "Characteristic function for x in [a;b) - Chi(x; a; b)";
        internal const String CHI_aB_DESC = "Characteristic function for x in (a;b] - cHi(x; a; b)";



    }


    internal sealed class SpecialFunction {


        internal const int TYPE_ID = 7;
        internal const int IFF_ID = 1;
        internal const int MIN_ID = 2;
        internal const int MAX_ID = 3;
        internal const int CONT_FRAC_ID = 4;
        internal const int CONT_POL_ID = 5;
        internal const int GCD_ID
            = 6;
        internal const int LCM_ID = 7;



        internal const String IFF_STR = "iff";
        internal const String MIN_STR = "min";
        internal const String MAX_STR = "max";
        internal const String CONT_FRAC_STR = "ConFrac";
        internal const String CONT_POL_STR = "ConPol";
        internal const String GCD_STR = "gcd";
        internal const String LCM_STR = "lcm";

        internal const String IFF_DESC = "if function ( iff(con_1; if_true_1_exp; ...; con_n; if_true_n_exp) )";
        internal const String MIN_DESC = "minimum function: min(a;b;c;...)";
        internal const String MAX_DESC = "maximum function: max(a;b;c;...)";
        internal const String CONT_FRAC_DESC = "Continued fraction: ConFrac(a;b;c;...)";
        internal const String CONT_POL_DESC = "Continued polynomial: ConPol(a;b;c;...)";
        internal const String GCD_DESC = "Greatest common divisor: gcd(a;b;c;...)";
        internal const String LCM_DESC = "Least common multiple: lcm(a;b;c;...)";



    }


    /**
     * Calculus
     * 
     * Identifiers and strings (words) definition.
     * Used mailny by the addParserKeywords() 
     */
    internal sealed class Calculus {


        internal const int TYPE_ID = 8;
        internal const int SUM_ID = 1;
        internal const int PROD_ID = 3;
        internal const int INT_ID = 5;
        internal const int DER_ID = 6;
        internal const int DER_LEFT_ID = 7;
        internal const int DER_RIGHT_ID = 8;
        internal const int DERN_ID = 9;
        internal const int FORW_DIFF_ID = 10;
        internal const int BACKW_DIFF_ID = 11;


        internal const String SUM_STR = "sum";
        internal const String PROD_STR = "prod";
        internal const String INT_STR = "int";
        internal const String DER_STR = "der";
        internal const String DER_LEFT_STR = "der-";
        internal const String DER_RIGHT_STR = "der+";
        internal const String DERN_STR = "dern";
        internal const String FORW_DIFF_STR = "diff";
        internal const String BACKW_DIFF_STR = "difb";

        internal const String SUM_DESC = "summation operator (SIGMA) sum(i; from; to; f(i;...))";
        internal const String PROD_DESC = "product operator (PI) prod(i; from; to; f(i;...))";
        internal const String INT_DESC = "definite integral operator ( int(f(x;...); x; a; b) )";
        internal const String DER_DESC = "derivative operator ( der(f(x;...); x) ) ";
        internal const String DER_LEFT_DESC = "left derivative operator ( der-(f(x;...); x) ) ";
        internal const String DER_RIGHT_DESC = "right derivative operator ( der+(f(x;...); x) ) ";
        internal const String DERN_DESC = "n-th derivative operator ( dern(f(x;...); x) ) ";
        internal const String FORW_DIFF_DESC = "forward difference operator";
        internal const String BACKW_DIFF_DESC = "backward difference operator";

    }

    /**
     * Const
     * 
     * Identifiers and strings (words) definition.
     * Used mailny by the addParserKeywords() 
     */
    internal sealed class Const {

        internal const int TYPE_ID = 10;

        internal const int PI_ID = 1;
        internal const int EULER_ID = 2;
        internal const int EULER_MASCHERONI_ID = 3;
        internal const int GOLDEN_RATIO_ID = 4;
        internal const int PLASTIC_ID = 5;
        internal const int EMBREE_TREFETHEN_ID = 6;
        internal const int FEIGENBAUM_DELTA_ID = 7;
        internal const int FEIGENBAUM_ALFA_ID = 8;
        internal const int TWIN_PRIME_ID = 9;
        internal const int MEISSEL_MERTEENS_ID = 10;
        internal const int BRAUN_TWIN_PRIME_ID = 11;
        internal const int BRAUN_PRIME_QUADR_ID = 12;
        internal const int BRUIJN_NEWMAN_ID = 13;
        internal const int CATALAN_ID = 14;
        internal const int LANDAU_RAMANUJAN_ID = 15;
        internal const int VISWANATH_ID = 16;
        internal const int LEGENDRE_ID = 17;
        internal const int RAMANUJAN_SOLDNER_ID = 18;
        internal const int ERDOS_BORWEIN_ID = 19;
        internal const int BERNSTEIN_ID = 20;
        internal const int GAUSS_KUZMIN_WIRSING_ID = 21;
        internal const int HAFNER_SARNAK_MCCURLEY_ID = 22;
        internal const int GOLOMB_DICKMAN_ID = 23;
        internal const int CAHEN_ID = 24;
        internal const int LAPLACE_LIMIT_ID = 25;
        internal const int ALLADI_GRINSTEAD_ID = 26;
        internal const int LENGYEL_ID = 27;
        internal const int LEVY_ID = 28;
        internal const int APERY_ID = 29;
        internal const int MILLS_ID = 30;
        internal const int BACKHOUSE_ID = 31;
        internal const int PORTER_ID = 32;
        internal const int LIEB_QUARE_ICE_ID = 33;
        internal const int NIVEN_ID = 34;
        internal const int SIERPINSKI_ID = 35;
        internal const int KHINCHIN_ID = 36;
        internal const int FRANSEN_ROBINSON_ID = 37;
        internal const int LANDAU_ID = 38;
        internal const int PARABOLIC_ID = 39;
        internal const int OMEGA_ID = 40;
        internal const int MRB_ID = 41;

        internal const int NaN = -1;


        internal const String PI_STR = "pi";
        internal const String EULER_STR = "e";
        internal const String EULER_MASCHERONI_STR = "[g]";
        internal const String GOLDEN_RATIO_STR = "[p]";
        internal const String PLASTIC_STR = "[r]";
        internal const String EMBREE_TREFETHEN_STR = "[B*]";
        internal const String FEIGENBAUM_DELTA_STR = "[D]";
        internal const String FEIGENBAUM_ALFA_STR = "[a]";
        internal const String TWIN_PRIME_STR = "[C2]";
        internal const String MEISSEL_MERTEENS_STR = "[M1]";
        internal const String BRAUN_TWIN_PRIME_STR = "[B2]";
        internal const String BRAUN_PRIME_QUADR_STR = "[B4]";
        internal const String BRUIJN_NEWMAN_STR = "[L]";
        internal const String CATALAN_STR = "[K]";
        internal const String LANDAU_RAMANUJAN_STR = "[K*]";
        internal const String VISWANATH_STR = "[K.]";
        internal const String LEGENDRE_STR = "[B'L]";
        internal const String RAMANUJAN_SOLDNER_STR = "[m]";
        internal const String ERDOS_BORWEIN_STR = "[EB]";
        internal const String BERNSTEIN_STR = "[B]";
        internal const String GAUSS_KUZMIN_WIRSING_STR = "[l]";
        internal const String HAFNER_SARNAK_MCCURLEY_STR = "[s]";
        internal const String GOLOMB_DICKMAN_STR = "[lm]";
        internal const String CAHEN_STR = "[C]";
        internal const String LAPLACE_LIMIT_STR = "[Ll]";
        internal const String ALLADI_GRINSTEAD_STR = "[AG]";
        internal const String LENGYEL_STR = "[L*]";
        internal const String LEVY_STR = "[L.]";
        internal const String APERY_STR = "[Dz3]";
        internal const String MILLS_STR = "[T]";
        internal const String BACKHOUSE_STR = "[Bh]";
        internal const String PORTER_STR = "[Pt]";
        internal const String LIEB_QUARE_ICE_STR = "[L2]";
        internal const String NIVEN_STR = "[Nv]";
        internal const String SIERPINSKI_STR = "[Ks]";
        internal const String KHINCHIN_STR = "[Kh]";
        internal const String FRANSEN_ROBINSON_STR = "[F]";
        internal const String LANDAU_STR = "[La]";
        internal const String PARABOLIC_STR = "[P2]";
        internal const String OMEGA_STR = "[O]";
        internal const String MRB_STR = "[M]";


        internal const String PI_DESC = "Pi, Archimedes' constant or Ludolph's number";
        internal const String EULER_DESC = "Napier's constant, or Euler's number, base of Natural logarithm";
        internal const String EULER_MASCHERONI_DESC = "Euler-Mascheroni constant";
        internal const String GOLDEN_RATIO_DESC = "Golden ratio";
        internal const String PLASTIC_DESC = "Plastic constant";
        internal const String EMBREE_TREFETHEN_DESC = "Embree-Trefethen constant";
        internal const String FEIGENBAUM_DELTA_DESC = "Feigenbaum constant alfa";
        internal const String FEIGENBAUM_ALFA_DESC = "Feigenbaum constant delta";
        internal const String TWIN_PRIME_DESC = "Twin prime constant";
        internal const String MEISSEL_MERTEENS_DESC = "Meissel-Mertens constant";
        internal const String BRAUN_TWIN_PRIME_DESC = "Brun's constant for twin primes";
        internal const String BRAUN_PRIME_QUADR_DESC = "Brun's constant for prime quadruplets";
        internal const String BRUIJN_NEWMAN_DESC = "de Bruijn-Newman constant";
        internal const String CATALAN_DESC = "Catalan's constant";
        internal const String LANDAU_RAMANUJAN_DESC = "Landau-Ramanujan constant";
        internal const String VISWANATH_DESC = "Viswanath's constant";
        internal const String LEGENDRE_DESC = "Legendre's constant";
        internal const String RAMANUJAN_SOLDNER_DESC = "Ramanujan-Soldner constant";
        internal const String ERDOS_BORWEIN_DESC = "Erdos�Borwein constant";
        internal const String BERNSTEIN_DESC = "Bernstein's constant";
        internal const String GAUSS_KUZMIN_WIRSING_DESC = "Gauss-Kuzmin-Wirsing constant";
        internal const String HAFNER_SARNAK_MCCURLEY_DESC = "Hafner-Sarnak-McCurley constant";
        internal const String GOLOMB_DICKMAN_DESC = "Golomb�Dickman constant";
        internal const String CAHEN_DESC = "Cahen's constant";
        internal const String LAPLACE_LIMIT_DESC = "Laplace limit";
        internal const String ALLADI_GRINSTEAD_DESC = "Alladi-Grinstead constant";
        internal const String LENGYEL_DESC = "Lengyel's constant";
        internal const String LEVY_DESC = "Levy's constant";
        internal const String APERY_DESC = "Apery's constant";
        internal const String MILLS_DESC = "Mills' constant";
        internal const String BACKHOUSE_DESC = "Backhouse's constant";
        internal const String PORTER_DESC = "Porter's constant";
        internal const String LIEB_QUARE_ICE_DESC = "Lieb's square ice constant";
        internal const String NIVEN_DESC = "Niven's constant";
        internal const String SIERPINSKI_DESC = "Sierpinski's constant";
        internal const String KHINCHIN_DESC = "Khinchin's constant";
        internal const String FRANSEN_ROBINSON_DESC = "Fransen-Robinson constant";
        internal const String LANDAU_DESC = "Landau's constant";
        internal const String PARABOLIC_DESC = "Parabolic constant";
        internal const String OMEGA_DESC = "Omega constant";
        internal const String MRB_DESC = "MRB constant";


    }


}