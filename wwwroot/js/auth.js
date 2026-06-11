window.submitLoginForm = function (email, senha) {
    const form = document.createElement("form");
    form.method = "POST";
    form.action = "/api/v1/auth/login";

    const inputEmail = document.createElement("input");
    inputEmail.type = "hidden";
    inputEmail.name = "email";
    inputEmail.value = email;
    form.appendChild(inputEmail);

    const inputSenha = document.createElement("input");
    inputSenha.type = "hidden";
    inputSenha.name = "senha";
    inputSenha.value = senha;
    form.appendChild(inputSenha);

    document.body.appendChild(form);
    form.submit();
};
