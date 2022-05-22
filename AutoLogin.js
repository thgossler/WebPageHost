//
// JavaScript for injection into any web page which finds a login form 
// and eventually fills-in username and password.
//
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
            let pwd = "$(PASSWORD)";
            let isPwdProvided = pwd.length > 0;
            setTimeout(function () {
                pwdElement.focus({ preventScroll: isPwdProvided });
                pwdElement.value = pwd;
            }, 1000);
            // Enter key press needs to be simulated outside script
        }
    }
}
