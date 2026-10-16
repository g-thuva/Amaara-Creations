let accessToken = null;
let authFailureHandler = null;

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
