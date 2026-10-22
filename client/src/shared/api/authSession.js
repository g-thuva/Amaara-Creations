let accessToken = null;
let authFailureHandler = null;
let authGeneration = 0;
export const beginAuthOperation = () => ++authGeneration;
export const currentAuthOperation = () => authGeneration;
export const isCurrentAuthOperation = ticket => ticket === authGeneration;

export const setAccessToken = (token) => {
  accessToken = token || null;
};

export const getAccessToken = () => accessToken;

export const clearAccessToken = () => {
  accessToken = null;
};

export const setAuthFailureHandler = (handler) => {
  authFailureHandler = handler;
};

export const notifyAuthFailure = () => {
  clearAccessToken();
  authFailureHandler?.();
};
