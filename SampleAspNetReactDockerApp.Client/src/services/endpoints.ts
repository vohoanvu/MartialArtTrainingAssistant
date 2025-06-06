/**
 * This file was auto-generated by openapi-typescript.
 * Do not make direct changes to the file.
 */


export interface paths {
  "/api/auth/v1/register": {
    post: {
      requestBody?: {
        content: {
          "application/json": components["schemas"]["RegisterRequest"];
        };
      };
      responses: {
        /** @description Success */
        200: {
          content: never;
        };
        /** @description Bad Request */
        400: {
          content: {
            "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
          };
        };
      };
    };
  };
  "/api/fighter/login": {
    post: {
      parameters: {
        query?: {
          useCookies?: boolean;
          useSessionCookies?: boolean;
        };
      };
      requestBody: {
        content: {
          "application/json": components["schemas"]["LoginRequest"];
        };
      };
      responses: {
        /** @description Success */
        200: {
          content: {
            "application/json": components["schemas"]["AccessTokenResponse"];
          };
        };
      };
    };
  };
  "/api/auth/v1/refresh": {
    post: {
      requestBody?: {
        content: {
          "application/json": components["schemas"]["RefreshRequest"];
        };
      };
      responses: {
        /** @description Success */
        200: {
          content: {
            "application/json": components["schemas"]["AccessTokenResponse"];
          };
        };
      };
    };
  };
  "/api/auth/v1/confirmEmail": {
    get: operations["MapIdentityApi-/api/auth/v1/confirmEmail"];
  };
  "/api/auth/v1/resendConfirmationEmail": {
    post: {
      requestBody?: {
        content: {
          "application/json": components["schemas"]["ResendConfirmationEmailRequest"];
        };
      };
      responses: {
        /** @description Success */
        200: {
          content: never;
        };
      };
    };
  };
  "/api/auth/v1/forgotPassword": {
    post: {
      requestBody?: {
        content: {
          "application/json": components["schemas"]["ForgotPasswordRequest"];
        };
      };
      responses: {
        /** @description Success */
        200: {
          content: never;
        };
        /** @description Bad Request */
        400: {
          content: {
            "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
          };
        };
      };
    };
  };
  "/api/auth/v1/resetPassword": {
    post: {
      requestBody?: {
        content: {
          "application/json": components["schemas"]["ResetPasswordRequest"];
        };
      };
      responses: {
        /** @description Success */
        200: {
          content: never;
        };
        /** @description Bad Request */
        400: {
          content: {
            "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
          };
        };
      };
    };
  };
  "/api/auth/v1/manage/2fa": {
    post: {
      requestBody?: {
        content: {
          "application/json": components["schemas"]["TwoFactorRequest"];
        };
      };
      responses: {
        /** @description Success */
        200: {
          content: {
            "application/json": components["schemas"]["TwoFactorResponse"];
          };
        };
        /** @description Bad Request */
        400: {
          content: {
            "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
          };
        };
        /** @description Unauthorized */
        401: {
          content: never;
        };
        /** @description Forbidden */
        403: {
          content: never;
        };
        /** @description Not Found */
        404: {
          content: never;
        };
      };
    };
  };
  "/api/auth/v1/manage/info": {
    get: {
      responses: {
        /** @description Success */
        200: {
          content: {
            "application/json": components["schemas"]["InfoResponse"];
          };
        };
        /** @description Bad Request */
        400: {
          content: {
            "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
          };
        };
        /** @description Unauthorized */
        401: {
          content: never;
        };
        /** @description Forbidden */
        403: {
          content: never;
        };
        /** @description Not Found */
        404: {
          content: never;
        };
      };
    };
    post: {
      requestBody?: {
        content: {
          "application/json": components["schemas"]["InfoRequest"];
        };
      };
      responses: {
        /** @description Success */
        200: {
          content: {
            "application/json": components["schemas"]["InfoResponse"];
          };
        };
        /** @description Bad Request */
        400: {
          content: {
            "application/problem+json": components["schemas"]["HttpValidationProblemDetails"];
          };
        };
        /** @description Unauthorized */
        401: {
          content: never;
        };
        /** @description Forbidden */
        403: {
          content: never;
        };
        /** @description Not Found */
        404: {
          content: never;
        };
      };
    };
  };
  "/api/v{version}/weatherforecast": {
    get: operations["GetWeatherForecast"];
  };
  "/api/video/metadata/{videoUrl}": {
    post: operations["UploadVideoMetadata"];
  }
}

export type webhooks = Record<string, never>;

export interface components {
  schemas: {
    AccessTokenResponse: {
      tokenType?: string | null;
      accessToken?: string | null;
      /** Format: int64 */
      expiresIn?: number;
      refreshToken?: string | null;
    };
    ForgotPasswordRequest: {
      email?: string | null;
    };
    HttpValidationProblemDetails: {
      type?: string | null;
      title?: string | null;
      /** Format: int32 */
      status?: number | null;
      detail?: string | null;
      instance?: string | null;
      errors?: {
        [key: string]: string[];
      } | null;
      [key: string]: unknown;
    };
    InfoRequest: {
      newEmail?: string | null;
      newPassword?: string | null;
      oldPassword?: string | null;
    };
    InfoResponse: {
      email?: string | null;
      isEmailConfirmed?: boolean;
    };
    LoginRequest: {
      email?: string | null;
      password?: string | null;
      twoFactorCode?: string | null;
      twoFactorRecoveryCode?: string | null;
    };
    RefreshRequest: {
      refreshToken?: string | null;
    };
    RegisterRequest: {
      email?: string | null;
      password?: string | null;
    };
    ResendConfirmationEmailRequest: {
      email?: string | null;
    };
    ResetPasswordRequest: {
      email?: string | null;
      resetCode?: string | null;
      newPassword?: string | null;
    };
    TwoFactorRequest: {
      enable?: boolean | null;
      twoFactorCode?: string | null;
      resetSharedKey?: boolean;
      resetRecoveryCodes?: boolean;
      forgetMachine?: boolean;
    };
    TwoFactorResponse: {
      sharedKey?: string | null;
      /** Format: int32 */
      recoveryCodesLeft?: number;
      recoveryCodes?: string[] | null;
      isTwoFactorEnabled?: boolean;
      isMachineRemembered?: boolean;
    };
    WeatherForecast: {
      /** Format: date */
      date?: string;
      /** Format: int32 */
      temperatureC?: number;
      /** Format: int32 */
      temperatureF?: number;
      summary?: string | null;
    };
    VideoMetadata: {
      id: string;
      videoId: string;
      title: string;
      description: string;
      embedLink?: string;
      sharedBy: {
        userId: string;
        username: string;
      }
    }
  };
  responses: never;
  parameters: never;
  requestBodies: never;
  headers: never;
  pathItems: never;
}

export type $defs = Record<string, never>;

export type external = Record<string, never>;

export interface operations {
  "MapIdentityApi-/api/auth/v1/confirmEmail": {
    parameters: {
      query?: {
        userId?: string;
        code?: string;
        changedEmail?: string;
      };
    };
    responses: {
      /** @description Success */
      200: {
        content: never;
      };
    };
  };
  GetWeatherForecast: {
    parameters: {
      path: {
        version: string;
      };
    };
    responses: {
      /** @description Success */
      200: {
        content: {
          "text/plain": components["schemas"]["WeatherForecast"][];
          "application/json": components["schemas"]["WeatherForecast"][];
          "text/json": components["schemas"]["WeatherForecast"][];
        };
      };
    };
  };

  UploadVideoMetadata: {
    /** @description Success */
    200: {
      content: {
        "text/plain": components["schemas"]["VideoMetadata"][];
        "application/json": components["schemas"]["VideoMetadata"][];
        "text/json": components["schemas"]["VideoMetadata"][];
      };
    };
  }
}
