(function ($) {
    if (!$ || !$.validator) return;
    $.extend($.validator.messages, {
        required: "Este campo es obligatorio.",
        email: "Ingresá un correo electrónico válido.",
        url: "Ingresá una URL válida.",
        date: "Ingresá una fecha válida.",
        number: "Ingresá un número válido.",
        digits: "Ingresá solo dígitos.",
        equalTo: "Los valores no coinciden.",
        maxlength: $.validator.format("No puede superar los {0} caracteres."),
        minlength: $.validator.format("Debe tener al menos {0} caracteres."),
        range: $.validator.format("Ingresá un valor entre {0} y {1}."),
        max: $.validator.format("Ingresá un valor menor o igual a {0}."),
        min: $.validator.format("Ingresá un valor mayor o igual a {0}.")
    });
})(window.jQuery);
