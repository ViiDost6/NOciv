using System;

// Este atributo nos permitirá "marcar" variables
// que queremos que aparezcan en nuestro editor de nodos.
[AttributeUsage(AttributeTargets.Field)] // Solo se puede usar en variables (campos)
public class ShowInEditorAttribute : Attribute
{
}