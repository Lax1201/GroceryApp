class AuthResponseModel {
  final String token;

  const AuthResponseModel({required this.token});

  factory AuthResponseModel.fromJson(Map<String, dynamic> json) {
    return AuthResponseModel(
      token: json['token'] as String? ?? '',
    );
  }

  Map<String, dynamic> toJson() => {
        'token': token,
      };
}
