const container = document.querySelector ('.container');
const signupbutton = document.querySelector('.signup-button');
const loginbutton = document.querySelector('.login-button');
signupbutton.addEventListener ('click', () => {
    container.classList.add('active');
});

loginbutton.addEventListener ('click', () => {
    container.classList.remove('active');
});