var inputs = document.getElementsByTagName('input');
var len = inputs.length;
var pwdElement = null;
while (len--) {
    if (inputs[len].type === 'password') {
        pwdElement = inputs[len];
    }
}
if (pwdElement) {
    var formElement = null;
    var elem = pwdElement;
    while (elem.parentNode) {
        if (elem.parentNode.nodeName.toLowerCase() === 'form') {
            formElement = elem.parentNode;
            break;
        }
        elem = elem.parentNode;
    }
    if (formElement) {
        inputs = formElement.getElementsByTagName('input');
        var len = inputs.length;
        var usrElement = null;
        while (len--) {
            if (inputs[len].type === 'email' || inputs[len].type === 'text') {
                usrElement = inputs[len];
            }
        }
        if (usrElement) {
            usrElement.focus({ preventScroll: true });
            usrElement.value = "$(USERNAME)";
            pwdElement.focus({ preventScroll: true });
            pwdElement.value = "$(PASSWORD)";
            // Enter key press needs to be simulated outside script
        }
    }
}
