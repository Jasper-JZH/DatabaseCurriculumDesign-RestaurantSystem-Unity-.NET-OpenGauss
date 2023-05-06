## 基于openGauss/.NET的餐饮服务系统

### 绪论

- **背景：**

  餐饮服务系统是从餐饮企业的特征和实际需求出发：为餐饮服务人员提供智能化高效管理等功能，为消费者提供舒适便捷点餐等服务的系统。系统适用于中餐酒楼、西餐厅、快餐、酒吧、茶楼、咖啡厅等大中小型餐饮企业。

- **开发环境：**

  操作系统：Windows 11、CentOS

  编译环境：.NET 6.0、C#

  编译工具：Visual Studio 2022

- **主要工作：**

  本文的主要工作是在CentOS系统上使用openGauss数据库设计了一个简易的餐饮服务数据库，并在Windows系统上采用C/S架构设计了一个.NET的后台服务器和基于Unity引擎实现的图形界面客户端。通过数据库、服务器和客户端三者的配合提供一个系统后台服务的模型，来模拟实现日常的餐饮企业基本业务。

  

>***本文档仅以图片形式呈现主要数据库结构和系统功能模块***

### 需求分析

![img](C:/Codes/LocalGitRes/DatabaseCourseDesign/README.assets/clip_image002.jpg)

![img](C:/Codes/LocalGitRes/DatabaseCourseDesign/README.assets/clip_image002-16833595100752.jpg)

![img](C:/Codes/LocalGitRes/DatabaseCourseDesign/README.assets/clip_image002-16833595166684.jpg)

![img](C:/Codes/LocalGitRes/DatabaseCourseDesign/README.assets/clip_image002-16833595241946.jpg)



### 概念结构

![img](C:/Codes/LocalGitRes/DatabaseCourseDesign/README.assets/clip_image002-16833595511588.jpg)



### 逻辑结构

![img](C:/Codes/LocalGitRes/DatabaseCourseDesign/README.assets/clip_image002-168335957176910.jpg)

![img](C:/Codes/LocalGitRes/DatabaseCourseDesign/README.assets/clip_image002-168335958040812.jpg)

![img](C:/Codes/LocalGitRes/DatabaseCourseDesign/README.assets/clip_image002-168335958676114.jpg)